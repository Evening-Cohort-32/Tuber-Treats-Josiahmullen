namespace TuberTreats.Models;

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Address { get; set; }
    public List<TuberOrder> TuberOrders { get; set; }
}
