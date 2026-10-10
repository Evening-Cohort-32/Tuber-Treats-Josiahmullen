namespace TuberTreats.Models;

public class TuberOrder
{
    public int Id { get; set; }
    public DateTime OrderPlacedOnDate { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; }
    // null until a driver is assigned to the order
    public int? TuberDriverId { get; set; }
    public TuberDriver TuberDriver { get; set; }
    // null until the order is delivered
    public DateTime? DeliveredOnDate { get; set; }
    public List<Topping> Toppings { get; set; }
}
