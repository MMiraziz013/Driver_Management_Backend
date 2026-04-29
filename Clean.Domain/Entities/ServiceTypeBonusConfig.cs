using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Clean.Domain.Enums;

namespace Clean.Domain.Entities;

public class ServiceTypeBonusConfig
{
    [Key]
    public int Id { get; set; }
    
    public int ServiceTypeId { get; set; }
    
    public BonusCalculationMethod CalculationMethod { get; set; }
    
    [ForeignKey(nameof(ServiceTypeId))]
    public ServiceType ServiceType { get; set; } = null!;
}