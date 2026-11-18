using TransportOrders.Web.Domain;

namespace TransportOrders.Web.Data;

/// <summary>Источник сведений об автомобилях.</summary>
public interface ICarSource
{
    IReadOnlyList<Car> All();
    Car? Find(int id);
}
