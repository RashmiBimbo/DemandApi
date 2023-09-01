using Microsoft.EntityFrameworkCore;
using System.Data.SqlClient;

[PrimaryKey(nameof(ReceiptId))]
public class Demand
{
    public int RecId { get; }
    public string SubcompanyId { get; set; }
    public string SalesPersonCode { get; set; }
    public string ReceiptId { get; set; }
    public DateTime DDate { get; set; }
    public int Mode { get; set; }
    public string CustomerId { get; set; }
    public double Opening { get; set; } = 0;
    public double SalesAmount { get; set; } = 0;
    public double TaxAmount { get; set; } = 0;
    public double ReceivedAmount { get; set; } = 0;
    public string? BankName { get; set; }
    public string? ChequeNumber { get; set; }
    public string? Description { get; set; }
    public int Active { get; set; } = 0;
    public DateTime CreationDateTime { get; set; } = DateTime.Today;
    public string? Lat { get; set; }
    public string? Long { get; set; }
}