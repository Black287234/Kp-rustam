-- 0003. Диспетчеризация: причина закрытия заявки, индекс для проверки занятости
-- и защита от двойного назначения автомобиля на уровне базы данных.

ALTER TABLE trip_request ADD COLUMN resolution TEXT;

UPDATE trip_request SET resolution = 'цель поездки не связана со служебными обязанностями' WHERE id = 3;
UPDATE trip_request SET resolution = 'поставщик заберёт продукцию сам' WHERE id = 8;

CREATE INDEX ix_trip_request_car_time ON trip_request (car_id, starts_at, ends_at);

-- Автомобиль не может быть занят двумя поездками в пересекающееся время.
-- Занятыми считаются заявки в статусах «автомобиль назначен» (2) и «в пути» (3).
CREATE TRIGGER trg_trip_request_car_busy
BEFORE UPDATE OF car_id, status, starts_at, ends_at ON trip_request
WHEN NEW.car_id IS NOT NULL AND NEW.status IN (2, 3)
BEGIN
    SELECT RAISE(ABORT, 'car_busy')
    WHERE EXISTS (
        SELECT 1 FROM trip_request o
        WHERE o.car_id = NEW.car_id
          AND o.id <> NEW.id
          AND o.status IN (2, 3)
          AND o.starts_at < NEW.ends_at
          AND NEW.starts_at < o.ends_at);
END;
