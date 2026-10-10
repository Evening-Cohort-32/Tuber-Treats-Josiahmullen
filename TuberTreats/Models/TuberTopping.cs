namespace TuberTreats.Models;

// join table between TuberOrder and Topping (an order can have many toppings,
// and a topping can be on many orders)
public class TuberTopping
{
    public int Id { get; set; }
    public int TuberOrderId { get; set; }
    public int ToppingId { get; set; }
}
