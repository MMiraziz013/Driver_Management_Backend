using Clean.Application.Dtos;
using Clean.Application.Dtos.Bonus;
using Clean.Application.Dtos.Responses;

namespace Clean.Application.Abstractions;

public interface IBonusCalculationService
{
    Task<Response<BonusCalculationResultDto>> CalculateBonusesAsync(BonusCalculationRequestDto request);
    Task<Response<byte[]>> ExportBonusesToExcelAsync(BonusCalculationRequestDto request);
}