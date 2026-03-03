using GuessItAPI.Models;
using GuessItAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GuessItAPI.Controllers
{
    [ApiController, Route("sub")]
    public class SubscribeController : ControllerBase
    {
        private readonly CardService _cardService;
        private readonly UserService _userService;

        public SubscribeController(CardService cardService, UserService userService)
        {
            _cardService = cardService;
            _userService = userService;
        }

        /// <summary>
        /// Добавляет пользователю подписку на категорию
        /// </summary>
        /// <param name="categoryId">id категории</param>
        [HttpPost, Route("subscribe"), Authorize]
        public async Task<ActionResult> Subscribe(int categoryId)
        {
            string username = HttpContext.User.FindFirst(ClaimTypes.Name)!.Value;
            User? user = await _userService.GetByUsername(username);
            if (user == null)
                return BadRequest();

            bool result = await _cardService.Subscribe(user.UserId, categoryId);
            if (!result)
                return BadRequest();
            return Ok();
        }

        /// <summary>
        /// Добавляет конкретному пользователю подписку на категорию(только для администратора)
        /// </summary>
        /// <param name="categoryId">id категории</param>
        /// <param name="auth">id или username пользователя</param>
        [HttpPost, Route("subscribeby"), Authorize(Roles = "admin")]
        public async Task<ActionResult> SubscribeBy(string auth, int categoryId)
        {
            User? user = await _userService.GetByAuth(auth);
            if (user == null)
                return BadRequest();

            bool result = await _cardService.Subscribe(user.UserId, categoryId);
            if (!result)
                return BadRequest();
            return Ok();
        }

        /// <summary>
        /// Удаляет у пользователя подписку на категорию
        /// </summary>
        /// <param name="categoryId">id категории</param>
        [HttpPost, Route("unsubscribe"), Authorize]
        public async Task<ActionResult> Unsubscribe(int categoryId)
        {
            string username = HttpContext.User.FindFirst(ClaimTypes.Name)!.Value;
            User? user = await _userService.GetByUsername(username);
            if (user == null)
                return BadRequest();

            bool result = await _cardService.Unsubscribe(user.UserId, categoryId);
            if (!result)
                return BadRequest();
            return Ok();
        }

        /// <summary>
        /// Удаляет у конкретного пользователя подписку на категорию (только для администратора)
        /// </summary>
        /// <param name="categoryId">id категории</param>
        /// <param name="auth">id или username пользователя</param>
        [HttpPost, Route("unsubscribeby"), Authorize(Roles = "admin")]
        public async Task<ActionResult> UnsubscribeBy(string auth, int categoryId)
        {
            User? user = await _userService.GetByAuth(auth);
            if (user == null)
                return BadRequest();

            bool result = await _cardService.Unsubscribe(user.UserId, categoryId);
            if (!result)
                return BadRequest();
            return Ok();
        }

        /// <summary>
        /// Возвращает все категории, на которые подписан данный пользователь
        /// </summary>
        /// <param name="includeCards">Включить ли связанные карты</param>
        [HttpGet, Route("getsubscriptions"), Authorize]
        public async Task<ActionResult<List<CardsCategory>>> GetAllSubscribedCategories(bool includeCards = false)
        {
            string username = HttpContext.User.FindFirst(ClaimTypes.Name)!.Value;
            User? user = await _userService.GetByUsername(username);

            if (user == null)
                return BadRequest();

            var categories = await _cardService.GetAllSubscribedCategories(user.UserId, includeCards);
            return Ok(new { categories = categories });
        }

        /// <summary>
        /// Возвращает все категории, на которые подписан другой пользователь
        /// </summary>
        /// <param name="includeCards">Включить ли связанные карты</param>
        /// /// <param name="auth">id или username пользователя</param>
        [HttpGet, Route("getsubscriptionsby"), Authorize]
        public async Task<ActionResult<List<CardsCategory>>> GetAllSubscribedCategoriesBy(string auth, bool includeCards = false)
        {
            Console.WriteLine(auth);
            User? user = await _userService.GetByAuth(auth);

            if (user == null)
                return BadRequest();

            var categories = await _cardService.GetAllSubscribedCategories(user.UserId, includeCards);
            return Ok(new { categories = categories });
        }

        /// <summary>
        /// Проверяет, подписан ли текущий пользователь на категорию
        /// </summary>
        /// <param name="categoryId">Id категории</param>
        [HttpGet, Route("checksubscription"), Authorize]
        public async Task<ActionResult> CheckSubscription(int categoryId)
        {
            string username = HttpContext.User.FindFirst(ClaimTypes.Name)!.Value;
            User? user = await _userService.GetByUsername(username);
            if (user == null)
                return BadRequest();

            bool result = await _cardService.CheckSubscription(user.UserId, categoryId);
            if (!result)
                return BadRequest();
            return Ok();
        }

        /// <summary>
        /// Проверяет, подписан ли указанный пользователь на категорию (только для администратора)
        /// </summary>
        /// <param name="auth">id или username пользователя</param>
        /// <param name="categoryId">id категории</param>
        [HttpGet, Route("checksubscriptionby"), Authorize(Roles = "admin")]
        public async Task<ActionResult> CheckSubscriptionBy(string auth, int categoryId)
        {
            User? user = await _userService.GetByAuth(auth);
            if (user == null)
                return BadRequest("User not found");

            bool result = await _cardService.CheckSubscription(user.UserId, categoryId);
            if (!result)
                return BadRequest();
            return Ok();
        }

    }
}
