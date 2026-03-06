using GuessItAPI.Models;
using GuessItAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace GuessItAPI.Controllers
{
    [ApiController, Route("card")]
    public class CardController : ControllerBase
    {
        private readonly CardService _cardService;
        private readonly UserService _userService;
        public CardController(CardService cardService, UserService userService)
        {
            _cardService = cardService;
            _userService = userService;
        }

        /// <summary>
        /// Создает новую карту в данной категории. Требует форму
        /// </summary>
        /// <param name="categoryId">Id категории</param>
        /// <param name="cardName">Имя карты</param>
        /// <returns>Id созданной карты при успехе, иначе код ошибки (BadRequest)</returns>
        [HttpPost, Route("create"), Authorize, RequestSizeLimit(15_000_000)]
        public async Task<ActionResult<int>> CreateCard(int categoryId, string? cardName = null)
        {
            if (!cardName.IsNullOrEmpty() && cardName.Length > 63)
                return BadRequest("Too long name");

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

                int result = await _cardService.CreateCard(categoryId, memoryStream.ToArray(), cardName);
                if (result == -1)
                    return BadRequest();
                return Ok(result);
            }
        }

        /// <summary>
        /// Меняет имя карты
        /// </summary>
        /// <param name="cardId">id карты</param>
        /// <param name="cardName">Имя карты</param>
        [HttpPatch, Route("change/name"), Authorize]
        public async Task<ActionResult> ChangeCardName(int cardId, string cardName)
        {
            if (cardName.Length > 63)
                return BadRequest("Too long name");

            Card? card = await _cardService.GetCard(cardId);
            if (card == null)
                return BadRequest();

            string username = HttpContext.User.FindFirst(ClaimTypes.Name)!.Value;
            bool? resultRights = await _cardService.CheckRights(card.CardCategoryId, username);
            if (!resultRights.HasValue)
                return BadRequest();
            if (resultRights == false)
                return Forbid();

            bool result = await _cardService.ChangeCardName(cardId, cardName);
            if (!result)
                return BadRequest();
            return Ok();
        }

        /// <summary>
        /// Меняет картинку карты. Требует форму
        /// </summary>
        /// <param name="cardId">id карты</param>
        [HttpPatch, Route("change/image"), Authorize, RequestSizeLimit(15_000_000)]
        public async Task<ActionResult> ChangeCardImage(int cardId)
        {
            Card? card = await _cardService.GetCard(cardId);
            if (card == null)
                return BadRequest();

            string username = HttpContext.User.FindFirst(ClaimTypes.Name)!.Value;
            bool? resultRights = await _cardService.CheckRights(card.CardCategoryId, username);
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

                bool result = await _cardService.ChangeCardImage(cardId, memoryStream.ToArray());

                if (!result)
                    return BadRequest();
                return Ok();
            }
        }

        /// <summary>
        /// Возвращает изображение карты по её ID
        /// </summary>
        /// <param name="cardId">ID карты</param>
        /// <response code="200">Файл-изображение</response>
        /// <response code="400">Карта не найдена или нет изображения</response>
        [HttpGet, Route("get/image/{cardId}"), Authorize]
        public async Task<IActionResult> GetCardImage(int cardId)
        {
            byte[]? imageBytes = await _cardService.GetCardImage(cardId);

            if (imageBytes == null || imageBytes.Length == 0)
                return BadRequest();

            string mimeType = CardService.GetMimeType(imageBytes);

            return File(imageBytes, mimeType);
        }
        /// <summary>
        /// Возвращает информацию карты по её ID
        /// </summary>
        /// <param name="cardId">ID карты</param>
        /// <response code="200">Информация о карте</response>
        /// <response code="400">Карта не найдена</response>
        [HttpGet, Route("get/info/{cardId}"), Authorize]
        public async Task<IActionResult> GetCardInfo(int cardId)
        {
            Card? card = await _cardService.GetCard(cardId);

            if (card == null)
                return BadRequest();
            return Ok(card);
        }

        /// <summary>
        /// Удалить карту
        /// </summary>
        /// <param name="cardId">Id карты</param>
        [HttpDelete, Route("delete"), Authorize]
        public async Task<ActionResult> DeleteCard(int cardId)
        {
            Card? card = await _cardService.GetCard(cardId);
            if (card == null)
                return BadRequest();

            string username = HttpContext.User.FindFirst(ClaimTypes.Name)!.Value;
            bool? resultRights = await _cardService.CheckRights(card.CardCategoryId, username);
            if (!resultRights.HasValue)
                return BadRequest();
            if (resultRights == false)
                return Forbid();

            bool result = await _cardService.DeleteCard(cardId);
            if (!result)
                return BadRequest();
            return Ok();
        }
    }
}
