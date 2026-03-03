using GuessItAPI.Models;
using GuessItAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace GuessItAPI.Controllers
{
    [ApiController, Route("category")]
    public class CategoryController : ControllerBase
    {
        private readonly CardService _cardService;
        private readonly UserService _userService;

        public CategoryController(CardService cardService, UserService userService)
        {
            _cardService = cardService;
            _userService = userService;
        }

        /// <summary>
        /// Создает новую категорию карт, владелец - данный пользователь
        /// </summary>
        /// <param name="categoryName">Имя категории</param>
        /// <returns>Id созданной категории при успехе, иначе код ошибки (BadRequest)</returns>
        [HttpPost, Route("сreate"), Authorize]
        [ApiExplorerSettings(GroupName = "v1")]
        public async Task<ActionResult<int>> CreateCategory(string categoryName)
        {
            if (categoryName.Length > 63)
                return BadRequest("Too long name");

            string username = HttpContext.User.FindFirst(ClaimTypes.Name)!.Value;
            User? user = await _userService.GetByUsername(username);
            if (user == null)
                return BadRequest();

            int id = await _cardService.CreateCategory(user.UserId, categoryName);
            if (id == -1)
                return BadRequest();
            return Ok(id);
        }

        /// <summary>
        /// Создает новую категорию карт(только для администратора)
        /// </summary>
        /// <param name="categoryName">Имя категории</param>
        /// <param name="auth">id или username владельца</param>
        /// <returns>Id созданной категории при успехе, иначе код ошибки (BadRequest)</returns>
        [HttpPost, Route("createby"), Authorize(Roles = "admin")]
        public async Task<ActionResult<int>> CreateCategory(string categoryName, string auth)
        {
            User? user = await _userService.GetByAuth(auth);
            if (user == null || categoryName.Length > 63)
                return BadRequest();

            int catId = await _cardService.CreateCategory(user.UserId, categoryName);
            if (catId == -1)
                return BadRequest();
            return Ok(catId);
        }

        /// <summary>
        /// Меняет описание категории
        /// </summary>
        /// <param name="categoryId">id категории</param>
        /// <param name="categoryDescription">Описание категории</param>
        [HttpPatch, Route("change/description"), Authorize]
        public async Task<ActionResult> ChangeDescription(int categoryId, string categoryDescription)
        {
            if (categoryDescription.Length > 65535)
                return BadRequest("Too long description");

            string username = HttpContext.User.FindFirst(ClaimTypes.Name)!.Value;
            bool? resultRights = await _cardService.CheckRights(categoryId, username);
            if (!resultRights.HasValue)
                return BadRequest();
            if (resultRights == false)
                return Forbid();

            bool result = await _cardService.ChangeDescription(categoryId, categoryDescription);
            if (!result)
                return BadRequest();
            return Ok();

        }

        /// <summary>
        /// Меняет имя категории
        /// </summary>
        /// <param name="categoryId">id категории</param>
        /// <param name="categoryName">Имя категории</param>
        [HttpPatch, Route("change/name"), Authorize]
        public async Task<ActionResult> ChangeCategoryName(int categoryId, string categoryName)
        {
            if (categoryName.Length > 63)
                return BadRequest("Too long name");

            string username = HttpContext.User.FindFirst(ClaimTypes.Name)!.Value;
            bool? resultRights = await _cardService.CheckRights(categoryId, username);
            if (!resultRights.HasValue)
                return BadRequest();
            if (resultRights == false)
                return Forbid();

            bool result = await _cardService.ChangeCategoryName(categoryId, categoryName);
            if (!result)
                return BadRequest();
            return Ok();
        }

        /// <summary>
        /// Добавляет картинку категории. Требует форму
        /// </summary>
        /// <param name="categoryId">id категории</param>
        [HttpPatch, Route("change/image"), Authorize, RequestSizeLimit(15_000_000)]
        public async Task<ActionResult> PutCategoryImage(int categoryId)
        {
            string username = HttpContext.User.FindFirst(ClaimTypes.Name)!.Value;
            bool? resultRights = await _cardService.CheckRights(categoryId, username);
            if (!resultRights.HasValue)
                return BadRequest();
            if (resultRights == false)
                return Forbid();

            var file = Request.Form.Files[0];

            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);

                bool result = await _cardService.PutCategoryImage(categoryId, memoryStream.ToArray());

                if (!result)
                    return BadRequest();
                return Ok();
            }
        }

        /// <summary>
        /// Удаление картинки группы
        /// </summary>
        /// <param name="categoryId">id категории</param>
        [HttpDelete, Route("delete/image"), Authorize]
        public async Task<ActionResult> DeleteCategoryImage(int categoryId)
        {
            string username = HttpContext.User.FindFirst(ClaimTypes.Name)!.Value;
            bool? resultRights = await _cardService.CheckRights(categoryId, username);
            if (!resultRights.HasValue)
                return BadRequest();
            if (resultRights == false)
                return Forbid();

            bool result = await _cardService.DeleteCategoryImage(categoryId);
            if (!result)
                return BadRequest();
            return Ok();
        }

        /// <summary>
        /// Возвращает изображение категории по её ID
        /// </summary>
        /// <param name="categoryId">ID категории</param>
        /// <response code="200">Файл-изображение</response>
        /// <response code="400">Категория не найдена или нет изображения</response>
        [HttpGet, Route("get/image/{categoryId}"), Authorize]
        public async Task<IActionResult> GetCategoryImage(int categoryId)
        {
            byte[]? imageBytes = await _cardService.GetCategoryImage(categoryId);

            if (imageBytes == null || imageBytes.Length == 0)
                return BadRequest();

            string mimeType = CardService.GetMimeType(imageBytes);

            return File(imageBytes, mimeType);
        }

        /// <summary>
        /// Определяет права на категорию
        /// </summary>
        /// <param name="categoryId">id категории</param>
        [HttpGet, Route("checkrights"), Authorize]
        public async Task<ActionResult> CheckRights(int categoryId)
        {
            string username = HttpContext.User.FindFirst(ClaimTypes.Name)!.Value;
            bool? resultRights = await _cardService.CheckRights(categoryId, username);
            if (!resultRights.HasValue)
                return BadRequest();
            if (resultRights == false)
                return Forbid();
            return Ok();
        }

        /// <summary>
        /// Получить всю информацию о категории
        /// </summary>
        /// <param name="categoryId">Id категории</param>
        /// <returns>Json категории, иначе код ошибки (BadRequest)</returns>
        [HttpGet, Route("get/info"), Authorize]
        public async Task<ActionResult<CardsCategory>> GetCategoryInfo(int categoryId)
        {
            CardsCategory? category = await _cardService.GetCategory(categoryId);
            if (category == null)
                return BadRequest();
            return Ok(new { category = category });
        }

        /// <summary>
        /// Получить всю информацию о категории и картах
        /// </summary>
        /// <param name="categoryId">Id категории</param>
        /// <returns>Json категории с картами, иначе код ошибки (BadRequest)</returns>
        [HttpGet, Route("get/infowithcards"), Authorize]
        public async Task<ActionResult<CardsCategory>> GetCategoryInfoWithCards(int categoryId)
        {
            CardsCategory? category = await _cardService.GetCategoryWithCards(categoryId);
            if (category == null)
                return BadRequest();
            return Ok(new { category = category });
        }
        /// <summary>
        /// Получить количество категорий
        /// </summary>
        /// <returns>Количество категорий на сервере(int)</returns>
        [HttpGet, Route("get/count"), Authorize]
        public async Task<ActionResult<int>> GetCategoriesCount()
        {
            int count = await _cardService.GetCategoriesCount();
            return Ok(count);
        }
        /// <summary>
        /// Получить информацию о 10 категориях
        /// </summary>
        /// <param name="index">Номер страницы</param>
        /// <returns>Десять категорий(или меньше, если в конце) по порядку согласно индексу</returns>
        /// <response code="200">Json с категориями</response>
        [HttpGet, Route("get/tencategories"), Authorize]
        public async Task<ActionResult<List<CardsCategory>>> GetTenCategories(int index)
        {
            List<CardsCategory> categories = await _cardService.GetTenCategories(index);
            if (categories.IsNullOrEmpty())
                return BadRequest();
            return Ok(new { categories = categories });
        }

        /// <summary>
        /// Ищет все категории, удовлетворяющие условиям поиска
        /// </summary>
        /// <param name="categoryId">ID категории</param>
        /// <param name="categoryName">Имя категории</param>
        /// <param name="ownerId">ID владельца категории</param>
        /// <param name="ownerName">Имя владельца категории</param>
        /// <param name="includeCards">Включить связанные карточки</param>
        /// <param name="showSubscribed">true — только подписанные, false — только неподписанные, null — все</param>
        /// <param name="userId">ID пользователя (взаимоисключимо с username)</param>
        /// <param name="username">Имя пользователя (взаимоисключимо с userId)</param>
        /// <param name="showOwnerInfo">Показывать владельца категории</param>
        /// <response code="200">Json с категориями</response>
        [HttpGet, Route("searchcategories"), Authorize]
        public async Task<ActionResult<List<CardsCategory>>> SearchCategories(
            [FromQuery] int? categoryId,
            [FromQuery] string? categoryName,
            [FromQuery] int? ownerId,
            [FromQuery] string? ownerName,
            [FromQuery] bool includeCards = false,
            [FromQuery] bool? showSubscribed = null,
            [FromQuery] int? userId = null,
            [FromQuery] string? username = null,
            [FromQuery] bool showOwnerInfo = false)
        {
            if (categoryId.HasValue && !string.IsNullOrEmpty(categoryName))
                return BadRequest("Укажите либо categoryId, либо categoryName, но не оба");

            if (ownerId.HasValue && !string.IsNullOrEmpty(ownerName))
                return BadRequest("Укажите либо ownerId, либо ownerName, но не оба");

            if (userId.HasValue && !string.IsNullOrEmpty(username))
                return BadRequest("Укажите либо userId, либо username, но не оба");

            string currentUsername = HttpContext.User.FindFirst(ClaimTypes.Name)!.Value;
            var currentUser = await _userService.GetByUsername(currentUsername);
            if (currentUser == null)
                return Unauthorized();

            int effectiveUserId;
            if (!string.IsNullOrEmpty(username))
            {
                var user = await _userService.GetByUsername(username);
                if (user == null)
                    return BadRequest($"Пользователь с именем '{username}' не найден");

                if (user.UserId != currentUser.UserId && currentUser.Role?.ToLower() != "admin")
                    return Forbid("Недостаточно прав для просмотра подписок другого пользователя");

                effectiveUserId = user.UserId;
            }
            else
            {
                effectiveUserId = userId ?? currentUser.UserId;
            }

            var categories = await _cardService.SearchCategories(
                categoryId,
                categoryName,
                ownerId,
                ownerName,
                effectiveUserId,
                showSubscribed,
                includeCards,
                showOwnerInfo);

            return Ok(new { categories });
        }

        /// <summary>
        /// Удалить категорию
        /// </summary>
        /// <param name="categoryId">Id категории</param>
        [HttpDelete, Route("delete"), Authorize]
        public async Task<ActionResult> DeleteCategory(int categoryId)
        {
            string username = HttpContext.User.FindFirst(ClaimTypes.Name)!.Value;
            bool? resultRights = await _cardService.CheckRights(categoryId, username);
            if (!resultRights.HasValue)
                return BadRequest();
            if (resultRights == false)
                return Forbid();

            bool result = await _cardService.DeleteCategory(categoryId);
            if (!result)
                return BadRequest();
            return Ok();
        }
    }
}
