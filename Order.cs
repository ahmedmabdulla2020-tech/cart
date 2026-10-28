namespace AzureCourse.Function
{
    public class Order
    {
        public string id { get; set; } = Guid.NewGuid().ToString();
        public int orderId { get; set; }
        public DateTime orderDate { get; set; }
        public double total { get; set; }
        public int priority  {get; set;}
        public List<OrderItem> items { get; set; }

        public Order()  {
            orderId=Random.Shared.Next(1000);
            priority=Random.Shared.Next(3)+1;
        }
    }

    public class OrderItem
    {
        public string name { get; set; }
        public int id { get; set; }
        public double price { get; set; }
    }
}
