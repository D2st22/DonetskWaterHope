using System.ComponentModel.DataAnnotations;

namespace ProjectsDonetskWaterHope.DTOs
{
	public record CreateTariffDto(
		[Required] string Name,
		[Range(0.01, 10000)] decimal PricePerUnit 
	);

	public record TariffDto(
		int TariffId,
		string Name,
		decimal PricePerUnit
	);
}