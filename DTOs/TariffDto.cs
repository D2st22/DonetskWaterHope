using System.ComponentModel.DataAnnotations;

namespace ProjectsDonetskWaterHope.DTOs
{
	// Вхідні дані (тільки те, що треба для створення)
	public record CreateTariffDto(
		[Required] string Name,
		[Range(0.01, 10000)] decimal PricePerUnit // Валідація: ціна не може бути 0 або від'ємною
	);

	// Вихідні дані (те, що бачить фронтенд)
	public record TariffDto(
		int TariffId,
		string Name,
		decimal PricePerUnit
	);
}