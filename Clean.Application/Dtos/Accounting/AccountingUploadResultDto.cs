namespace Clean.Application.Dtos.Accounting;

public class AccountingUploadResultDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int TransactionsImported { get; set; }
    public int TransactionsSkipped { get; set; }
    public decimal TotalAmount { get; set; }
    public List<string> Warnings { get; set; } = new();
}