using GuessItAPI.Context;
using GuessItAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace GuessItAPI.Services
{
    public class CardService
    {
        private readonly GuessItDbContext _dbContext;
        private readonly UserService _userService;
        public CardService(GuessItDbContext db, UserService userService)
        {
            _dbContext = db;
            _userService = userService;
        }

        public async Task<int> CreateCategory(int userId, string categoryName)
        {
            User? user = await _userService.GetById(userId);
            if (user == null)
                return -1;

            CardsCategory category = new CardsCategory();
            category.CategoryName = categoryName;
            category.CategoryOwnerId = userId;
            _dbContext.CardsCategories.Add(category);
            await _dbContext.SaveChangesAsync();
            return category.CategoryId;
        }
        public async Task<int> GetCategoriesCount() => await _dbContext.CardsCategories.CountAsync();
        public async Task<CardsCategory?> GetCategory(int categoryId)
        {
            CardsCategory? category = await _dbContext.CardsCategories.SingleOrDefaultAsync(c => c.CategoryId == categoryId);
            return category;
        }

        public async Task<CardsCategory?> GetCategoryWithCards(int categoryId)
        {
            var categoryData = await _dbContext.CardsCategories
                .AsNoTracking()
                .Where(c => c.CategoryId == categoryId)
                .Select(c => new
                {
                    CategoryId = c.CategoryId,
                    CategoryName = c.CategoryName,
                    CategoryOwnerId = c.CategoryOwnerId,
                    CategoryDescription = c.CategoryDescription,
                    Cards = c.Cards.Select(card => new
                    {
                        CardId = card.CardId,
                        CardCategoryId = card.CardCategoryId,
                        CardName = card.CardName,
                        CardImagePreview = card.CardImagePreview
                    }).ToList()
                })
                .SingleOrDefaultAsync();

            if (categoryData == null)
                return null;

            var cardsWithoutPreviewIds = categoryData.Cards
                .Where(c => c.CardImagePreview == null || c.CardImagePreview.Length == 0)
                .Select(c => c.CardId)
                .ToList();

            Dictionary<int, byte[]> generatedPreviews = new();

            if (cardsWithoutPreviewIds.Count > 0)
            {
                var cardsToUpdate = await _dbContext.Cards
                    .Where(c => cardsWithoutPreviewIds.Contains(c.CardId))
                    .ToListAsync();

                bool changed = false;

                foreach (var card in cardsToUpdate)
                {
                    if (card.CardImage != null && card.CardImage.Length > 0)
                    {
                        card.CardImagePreview = ImageHelper.ResizeImage(card.CardImage, 256);
                        generatedPreviews[card.CardId] = card.CardImagePreview;
                        changed = true;
                    }
                }

                if (changed)
                    await _dbContext.SaveChangesAsync();
            }

            var result = new CardsCategory
            {
                CategoryId = categoryData.CategoryId,
                CategoryName = categoryData.CategoryName,
                CategoryDescription = categoryData.CategoryDescription,
                CategoryOwnerId = categoryData.CategoryOwnerId,
                Cards = categoryData.Cards.Select(cardData => new Card
                {
                    CardId = cardData.CardId,
                    CardCategoryId = cardData.CardCategoryId,
                    CardName = cardData.CardName,
                    CardImagePreview = (cardData.CardImagePreview != null && cardData.CardImagePreview.Length > 0)
                        ? cardData.CardImagePreview
                        : generatedPreviews.GetValueOrDefault(cardData.CardId),
                    CardImage = null,
                    CardCategory = null
                }).ToList()
            };

            return result;
        }

        public async Task<List<CardsCategory>> GetAllCategoriesByOwner(int ownerId)
        {
            User? user = await _userService.GetById(ownerId);
            if (user == null)
                return new List<CardsCategory>();

            List<CardsCategory> cardsCategories = await _dbContext.CardsCategories.Where(c => c.CategoryOwnerId == ownerId).ToListAsync();
            cardsCategories.ForEach(c => c.CategoryImage = Array.Empty<byte>());
            return cardsCategories;
        }
        public async Task<List<CardsCategory>> GetTenCategories(int index)
        {
            const int pageSize = 10;

            int totalCategories = await _dbContext.CardsCategories.CountAsync();
            int skip = index * pageSize;

            if (skip >= totalCategories)
                return new List<CardsCategory>();

            int take = Math.Min(pageSize, totalCategories - skip);

            var categories = await _dbContext.CardsCategories
                .OrderBy(c => c.CategoryId)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
            categories.ForEach(c => c.CategoryImage = Array.Empty<byte>());

            return categories;
        }
        public async Task<List<CardsCategory>> SearchCategories(
    int? categoryId,
    string? categoryName,
    int? ownerId,
    string? ownerName,
    int userId,
    bool? showSubscribed = null,
    bool includeCards = false,
    bool showOwnerInfo = false)
        {
            var query = _dbContext.CardsCategories
                .Include(c => c.CategoryOwner)
                .Include(c => c.UserSubscribes)
                .AsQueryable();

            if (includeCards)
                query = query.Include(c => c.Cards);

            if (categoryId.HasValue)
                query = query.Where(c => c.CategoryId == categoryId.Value);
            else if (!string.IsNullOrEmpty(categoryName))
                query = query.Where(c => EF.Functions.Like(c.CategoryName, $"%{categoryName}%"));

            if (ownerId.HasValue)
                query = query.Where(c => c.CategoryOwnerId == ownerId.Value);
            else if (!string.IsNullOrEmpty(ownerName))
                query = query.Where(c => EF.Functions.Like(c.CategoryOwner.Username, $"%{ownerName}%"));

            if (showSubscribed.HasValue)
            {
                if (showSubscribed.Value)
                    query = query.Where(c => c.UserSubscribes.Any(s => s.UserId == userId));
                else
                    query = query.Where(c => c.UserSubscribes.All(s => s.UserId != userId));
            }

            var categories = await query.ToListAsync();

            foreach (var category in categories)
            {
                category.CategoryImage = Array.Empty<byte>();

                if (!showOwnerInfo)
                {
                    category.CategoryOwner = null;
                }
                else if (category.CategoryOwner != null)
                {
                    category.CategoryOwner.CardsCategories = null;
                    category.CategoryOwner.UserSubscribes = null;
                }

                if (category.UserSubscribes != null)
                {
                    foreach (var sub in category.UserSubscribes)
                    {
                        sub.Category = null;
                        sub.User = null;
                    }
                    category.UserSubscribes?.Clear();
                }

                if (includeCards && category.Cards != null)
                {
                    foreach (var card in category.Cards)
                    {
                        card.CardImage = Array.Empty<byte>();
                        card.CardCategory = null;
                    }
                }
            }

            return categories;
        }



        public async Task<bool> ChangeDescription(int categoryId, string description)
        {
            var category = await GetCategory(categoryId);
            if (category == null)
                return false;

            category.CategoryDescription = description;
            await _dbContext.SaveChangesAsync();
            return true;
        }
        public async Task<bool> DeleteCategory(int categoryId)
        {
            var category = await GetCategory(categoryId);
            if (category == null)
                return false;

            var cardsData = _dbContext.Cards
                .Where(c => c.CardCategoryId == category.CategoryId);

            if (await cardsData.AnyAsync())
            {
                _dbContext.Cards.RemoveRange(cardsData);
            }

            _dbContext.CardsCategories.Remove(category);
            await _dbContext.SaveChangesAsync();
            return true;
        }
        public async Task<bool> PutCategoryImage(int categoryId, byte[] image)
        {
            CardsCategory? category = await GetCategory(categoryId);
            if (category == null)
                return false;

            category.CategoryImage = image;
            await _dbContext.SaveChangesAsync();
            return true;
        }
        public async Task<bool> DeleteCategoryImage(int categoryId)
        {
            CardsCategory? category = await GetCategory(categoryId);
            if (category == null)
                return false;

            UserDatum? userDatumModel = await _dbContext.UserData.FirstOrDefaultAsync(u => u.UserId == 1);
            category.CategoryImage = userDatumModel.Avatar;
            await _dbContext.SaveChangesAsync();
            return true;
        }
        public async Task<byte[]?> GetCategoryImage(int categoryId)
        {
            CardsCategory? category = await _dbContext.CardsCategories.SingleOrDefaultAsync(c => c.CategoryId == categoryId);
            if (category == null)
                return Array.Empty<byte>();

            return category.CategoryImage;
        }
        public async Task<bool> ChangeCategoryName(int categoryId, string newName)
        {
            var category = await GetCategory(categoryId);
            if (category == null)
                return false;

            category.CategoryName = newName;
            await _dbContext.SaveChangesAsync();
            return true;
        }
        public async Task<int> CreateCard(int categoryId, byte[] cardImage, string? name = null)
        {
            var category = await GetCategory(categoryId);
            if (category == null)
                return -1;

            Card card = new Card();
            card.CardCategory = category;
            card.CardImage = cardImage;
            card.CardImagePreview = ImageHelper.ResizeImage(cardImage, 256);
            card.CardName = name;
            _dbContext.Cards.Add(card);

            await _dbContext.SaveChangesAsync();
            return card.CardId;
        }
        public async Task<Card?> GetCard(int cardId) => await _dbContext.Cards.SingleOrDefaultAsync(c => c.CardId == cardId);
        public async Task<bool> ChangeCardName(int cardId, string newName)
        {
            var card = await GetCard(cardId);
            if (card == null)
                return false;

            card.CardName = newName;
            await _dbContext.SaveChangesAsync();
            return true;
        }
        public async Task<bool> ChangeCardImage(int cardId, byte[] image)
        {
            var card = await GetCard(cardId);
            if (card == null)
                return false;

            card.CardImage = image;
            card.CardImagePreview = ImageHelper.ResizeImage(image, 256);
            await _dbContext.SaveChangesAsync();
            return true;
        }
        public async Task<byte[]?> GetCardImage(int cardId)
        {
            var cardData = await _dbContext.Cards
                .Where(c => c.CardId == cardId)
                .Select(c => new
                {
                    c.CardId,
                    c.CardImagePreview
                })
                .SingleOrDefaultAsync();

            if (cardData == null)
                return null;

            if (cardData.CardImagePreview != null && cardData.CardImagePreview.Length > 0)
                return cardData.CardImagePreview;

            var card = await _dbContext.Cards
                .SingleOrDefaultAsync(c => c.CardId == cardId);

            if (card == null || card.CardImage == null || card.CardImage.Length == 0)
                return null;

            card.CardImagePreview = ImageHelper.ResizeImage(card.CardImage, 256);
            await _dbContext.SaveChangesAsync();

            return card.CardImagePreview;
        }
        public async Task<bool> DeleteCard(int cardId)
        {
            var card = await GetCard(cardId);
            if (card == null)
                return false;

            _dbContext.Cards.Remove(card);
            await _dbContext.SaveChangesAsync();
            return true;
        }
        public async Task<bool> Subscribe(int userId, int categoryId)
        {
            var category = await GetCategory(categoryId);
            if (category == null)
                return false;

            User? user = await _userService.GetById(userId);
            if (user == null)
                return false;

            if (await _dbContext.UserSubscribes.AnyAsync(us => us.UserId == userId && us.CategoryId == categoryId))
                return true;

            UserSubscribe subscribe = new UserSubscribe
            {
                UserId = userId,
                CategoryId = categoryId
            };

            _dbContext.UserSubscribes.Add(subscribe);
            await _dbContext.SaveChangesAsync();
            return true;
        }
        public async Task<bool> Unsubscribe(int userId, int categoryId)
        {
            var category = await GetCategory(categoryId);
            if (category == null)
                return false;

            var user = await _userService.GetById(userId);
            if (user == null)
                return false;

            var subscription = await _dbContext.UserSubscribes
                .FirstOrDefaultAsync(us => us.UserId == userId && us.CategoryId == categoryId);

            if (subscription == null)
                return false;

            _dbContext.UserSubscribes.Remove(subscription);
            await _dbContext.SaveChangesAsync();
            return true;
        }
        public async Task<List<CardsCategory>> GetAllSubscribedCategories(int userId, bool includeCards = false)
        {
            IQueryable<CardsCategory> query = _dbContext.UserSubscribes
                .Where(us => us.UserId == userId)
                .Select(us => us.Category);

            if (includeCards)
            {
                query = query.Include(c => c.Cards);
            }

            var categories = await query.ToListAsync();

            foreach (var category in categories)
            {
                category.CategoryImage = Array.Empty<byte>();
                category.CategoryOwner = null;

                if (includeCards && category.Cards != null)
                {
                    foreach (var card in category.Cards)
                    {
                        card.CardImage = Array.Empty<byte>();
                        card.CardCategory = null;
                    }
                }
            }

            return categories;
        }

        public async Task<bool?> CheckRights(int categoryId, string username)
        {
            User? user = await _userService.GetByUsername(username);
            CardsCategory? category = await GetCategory(categoryId);
            if (user == null || category == null)
                return null;
            if (category.CategoryOwnerId != user.UserId && user.Role != "admin")
                return false;
            return true;
        }
        public async Task<bool?> CheckRights(int categoryId, User? user)
        {
            CardsCategory? category = await GetCategory(categoryId);
            if (user == null || category == null)
                return null;
            if (category.CategoryOwnerId != user.UserId && user.Role != "admin")
                return false;
            return true;
        }
        public async Task<bool> CheckSubscription(int userId, int categoryId)
        {
            return await _dbContext.UserSubscribes
                .AnyAsync(us => us.UserId == userId && us.CategoryId == categoryId);
        }
        public static string GetMimeType(byte[] imageData)
        {
            // JPEG: FF D8
            if (imageData.Length >= 2 && imageData[0] == 0xFF && imageData[1] == 0xD8)
                return "image/jpeg";

            // PNG: 89 50 4E 47 0D 0A 1A 0A
            if (imageData.Length >= 8 &&
                imageData[0] == 0x89 && imageData[1] == 0x50 &&
                imageData[2] == 0x4E && imageData[3] == 0x47 &&
                imageData[4] == 0x0D && imageData[5] == 0x0A &&
                imageData[6] == 0x1A && imageData[7] == 0x0A)
                return "image/png";

            // GIF: 47 49 46
            if (imageData.Length >= 3 && imageData[0] == 0x47 && imageData[1] == 0x49 && imageData[2] == 0x46)
                return "image/gif";

            // BMP: 42 4D
            if (imageData.Length >= 2 && imageData[0] == 0x42 && imageData[1] == 0x4D)
                return "image/bmp";

            // WebP: 52 49 46 46 .... 57 45 42 50
            if (imageData.Length >= 12 &&
                imageData[0] == 0x52 && imageData[1] == 0x49 && imageData[2] == 0x46 && imageData[3] == 0x46 &&
                imageData[8] == 0x57 && imageData[9] == 0x45 && imageData[10] == 0x42 && imageData[11] == 0x50)
                return "image/webp";

            // По умолчанию
            return "application/octet-stream";
        }

    }
}
