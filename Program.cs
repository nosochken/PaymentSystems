using System.Security.Cryptography;
using System.Text;

class Program
{
    static void Main(string[] args)
    {
        //Выведите платёжные ссылки для трёх разных систем платежа: 
        //pay.system1.ru/order?amount=12000RUB&hash={MD5 хеш ID заказа}
        //order.system2.ru/pay?hash={MD5 хеш ID заказа + сумма заказа}
        //system3.com/pay?amount=12000&curency=RUB&hash={SHA-1 хеш сумма заказа + ID заказа + секретный ключ от системы}

        Hasher MD5Hasher = new Hasher(MD5.Create());
        Hasher SHA1Hasher = new Hasher(SHA1.Create());

        Order order = new Order(1275, 12000);

        int firstSecretKeyNumber = 0;
        int lastSecretKeyNumber = 200;

        List<IPaymentSystem> paymentSystems = new List<IPaymentSystem>
        {
            new FirstPaymentSystem(MD5Hasher),
            new SecondPaymentSystem(MD5Hasher),
            new ThirdPaymentSystem(SHA1Hasher, firstSecretKeyNumber, lastSecretKeyNumber)
        };

        foreach (IPaymentSystem paymentSystem in paymentSystems)
            Console.WriteLine(paymentSystem.GetPayingLink(order));
    }
}

public class Order
{
    public Order(int id, int amount)
    {
        if (id < 0)
            throw new ArgumentOutOfRangeException(nameof(id));

        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount));

        Id = id;
        Amount = amount;
    }

    public int Id { get; }
    public int Amount { get; }
}

public interface IPaymentSystem
{
    public string GetPayingLink(Order order);
}

public class FirstPaymentSystem : IPaymentSystem
{
    private Hasher _hasher;

    public FirstPaymentSystem(Hasher hasher)
    {
        _hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
    }

    public string GetPayingLink(Order order)
    {
        if (order == null)
            throw new ArgumentNullException(nameof(order));

        return $"pay.system1.ru/order?amount={order.Amount}RUB&hash={_hasher.GetHash(order.Id.ToString())}";
    }
}

public class SecondPaymentSystem : IPaymentSystem
{
    private Hasher _hasher;

    public SecondPaymentSystem(Hasher hasher)
    {
        _hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
    }

    public string GetPayingLink(Order order)
    {
        if (order == null)
            throw new ArgumentNullException(nameof(order));

        return $"order.system2.ru/pay?hash={_hasher.GetHash(order.Id.ToString() + order.Amount.ToString())}";
    }
}

public class ThirdPaymentSystem : IPaymentSystem
{
    private Hasher _hasher;

    private int _firstSecretKeyNumber;
    private int _lastSecretKeyNumber;

    public ThirdPaymentSystem(Hasher hasher, int firstSecretKeyNumber, int lastSecretKeyNumber)
    {
        _hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));

        if (firstSecretKeyNumber < 0)
            throw new ArgumentOutOfRangeException(nameof(firstSecretKeyNumber));

        if (lastSecretKeyNumber < firstSecretKeyNumber)
            throw new ArgumentOutOfRangeException(nameof(lastSecretKeyNumber));

        _firstSecretKeyNumber = firstSecretKeyNumber;
        _lastSecretKeyNumber = lastSecretKeyNumber;
    }

    public string GetPayingLink(Order order)
    {
        if (order == null)
            throw new ArgumentNullException(nameof(order));

        return $"system3.com/pay?amount={order.Amount}&curency=RUB&hash={_hasher.GetHash(order.Amount.ToString() + order.Id.ToString() + GetSecretKey().ToString())}";
    }

    private int GetSecretKey()
    {
        Random random = new Random();

        return random.Next(_firstSecretKeyNumber, _lastSecretKeyNumber + 1);
    }
}

public class Hasher
{
    private HashAlgorithm _hashAlgorithm;

    public Hasher(HashAlgorithm hashAlgorithm)
    {
        _hashAlgorithm = hashAlgorithm ?? throw new ArgumentNullException(nameof(hashAlgorithm)); ;
    }

    public string GetHash(string data)
    {
        if (string.IsNullOrWhiteSpace(data))
            throw new ArgumentException(nameof(data));

        byte[] dataBytes = Encoding.ASCII.GetBytes(data);
        byte[] hash = _hashAlgorithm.ComputeHash(dataBytes);

        return Convert.ToHexString(hash);
    }
}