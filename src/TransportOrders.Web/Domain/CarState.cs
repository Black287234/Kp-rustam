namespace TransportOrders.Web.Domain;

/// <summary>Техническое состояние автомобиля в автопарке.</summary>
public enum CarState
{
    /// <summary>Исправен, может назначаться на поездки.</summary>
    Operational = 1,

    /// <summary>Находится на ремонте или обслуживании.</summary>
    InRepair = 2,

    /// <summary>Выведен из автопарка, запись сохраняется для истории.</summary>
    Decommissioned = 3
}
