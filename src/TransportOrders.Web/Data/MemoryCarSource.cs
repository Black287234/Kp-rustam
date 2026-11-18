using TransportOrders.Web.Domain;

namespace TransportOrders.Web.Data;

/// <summary>
/// Учебный автопарк в оперативной памяти (этап 1).
/// Данные создаются при запуске и не сохраняются между перезапусками.
/// </summary>
public sealed class MemoryCarSource : ICarSource
{
    private readonly List<Car> _cars =
    [
        new() { Id = 1, Plate = "А214КМ 186", Make = "LADA", Model = "Vesta", Kind = CarKind.Sedan, PassengerSeats = 4, ReleaseYear = 2021 },
        new() { Id = 2, Plate = "В530ОР 186", Make = "Toyota", Model = "Camry", Kind = CarKind.Sedan, PassengerSeats = 4, ReleaseYear = 2020 },
        new() { Id = 3, Plate = "Е771ТН 86", Make = "Hyundai", Model = "H-1", Kind = CarKind.Minibus, PassengerSeats = 7, ReleaseYear = 2019 },
        new() { Id = 4, Plate = "К408СУ 186", Make = "ГАЗ", Model = "Газель Next", Kind = CarKind.Minibus, PassengerSeats = 15, ReleaseYear = 2022 },
        new() { Id = 5, Plate = "М126АХ 186", Make = "Škoda", Model = "Octavia", Kind = CarKind.Sedan, PassengerSeats = 4, ReleaseYear = 2022 },
        new() { Id = 6, Plate = "Н903ЕВ 86", Make = "Ford", Model = "Transit", Kind = CarKind.Van, PassengerSeats = 2, ReleaseYear = 2018, State = CarState.InRepair },
        new() { Id = 7, Plate = "Р345ЛА 186", Make = "Renault", Model = "Logan", Kind = CarKind.Sedan, PassengerSeats = 4, ReleaseYear = 2014, State = CarState.Decommissioned },
        new() { Id = 8, Plate = "Т612ОС 186", Make = "KIA", Model = "Carnival", Kind = CarKind.Minibus, PassengerSeats = 7, ReleaseYear = 2023 }
    ];

    public IReadOnlyList<Car> All() => _cars.OrderBy(c => c.Plate, StringComparer.Ordinal).ToList();

    public Car? Find(int id) => _cars.FirstOrDefault(c => c.Id == id);
}
