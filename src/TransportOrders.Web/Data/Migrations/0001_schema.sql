-- 0001. Структура базы модуля заявок на служебный транспорт

CREATE TABLE car (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    plate           TEXT    NOT NULL COLLATE NOCASE UNIQUE,
    make            TEXT    NOT NULL,
    model           TEXT    NOT NULL,
    kind            INTEGER NOT NULL CHECK (kind IN (1, 2, 3)),
    passenger_seats INTEGER NOT NULL CHECK (passenger_seats BETWEEN 1 AND 30),
    release_year    INTEGER NOT NULL CHECK (release_year BETWEEN 1990 AND 2100),
    state           INTEGER NOT NULL DEFAULT 1 CHECK (state IN (1, 2, 3))
);

CREATE TABLE employee (
    id           INTEGER PRIMARY KEY AUTOINCREMENT,
    personnel_no TEXT    NOT NULL UNIQUE CHECK (length(personnel_no) = 5),
    full_name    TEXT    NOT NULL,
    department   TEXT    NOT NULL,
    position     TEXT    NOT NULL,
    phone        TEXT,
    is_active    INTEGER NOT NULL DEFAULT 1 CHECK (is_active IN (0, 1))
);

-- Заявка на поездку. Время хранится строкой «ГГГГ-ММ-ДДTчч:мм» (местное время организации).
CREATE TABLE trip_request (
    id           INTEGER PRIMARY KEY AUTOINCREMENT,
    requester_id INTEGER NOT NULL REFERENCES employee (id),
    starts_at    TEXT    NOT NULL,
    ends_at      TEXT    NOT NULL,
    origin       TEXT    NOT NULL,
    destination  TEXT    NOT NULL,
    purpose      TEXT    NOT NULL,
    passengers   INTEGER NOT NULL CHECK (passengers >= 1),
    status       INTEGER NOT NULL DEFAULT 1 CHECK (status BETWEEN 1 AND 6),
    car_id       INTEGER REFERENCES car (id),
    created_at   TEXT    NOT NULL,
    CHECK (ends_at > starts_at)
);

-- Журнал заявки: создание, назначение автомобиля, смена статуса, комментарии диспетчера
CREATE TABLE trip_log (
    id         INTEGER PRIMARY KEY AUTOINCREMENT,
    request_id INTEGER NOT NULL REFERENCES trip_request (id),
    logged_at  TEXT    NOT NULL,
    status     INTEGER NOT NULL,
    message    TEXT    NOT NULL
);

CREATE INDEX ix_trip_request_status ON trip_request (status);
CREATE INDEX ix_trip_log_request ON trip_log (request_id);
