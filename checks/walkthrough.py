"""
Проверки модуля через интерфейс в браузере (Playwright).

Запуск: приложение запущено на чистой учебной базе (папка App_Data удалена), затем
    python checks/walkthrough.py <папка_для_результатов> [--restart "команда запуска"]

Для каждой проверки записываются исходные данные, действия, ожидаемый и фактический результат,
итог и снимок экрана. Результаты сохраняются в results.json, снимки — в папку shots.
"""
import json
import subprocess
import sys
import time
import urllib.request
from datetime import date, timedelta
from pathlib import Path

from playwright.sync_api import sync_playwright

BASE = "http://localhost:5113"
OUT = Path(sys.argv[1] if len(sys.argv) > 1 else "docs/checks")
SHOTS = OUT / "shots"
SHOTS.mkdir(parents=True, exist_ok=True)
TOMORROW = date.today() + timedelta(days=1)
YESTERDAY = date.today() - timedelta(days=1)

results = []


def check(code, group, title, data, steps, expected):
    """Декоратор: регистрирует проверку. Функция проверки возвращает (фактический результат, пройдено?)."""
    def wrap(fn):
        fn.meta = dict(code=code, group=group, title=title, data=data, steps=steps, expected=expected)
        CHECKS.append(fn)
        return fn
    return wrap


CHECKS = []


def flash(page):
    box = page.locator(".flash")
    return " | ".join(t.strip() for t in box.all_inner_texts()) if box.count() else ""


def errors(page):
    items = page.locator(".err:not(:empty), .errors li").all_inner_texts()
    return [t.strip() for t in items if t.strip()]


def submit(page, selector="form.form button[type=submit]"):
    page.locator(selector).first.click()
    page.wait_for_load_state("networkidle")


def fill_trip(page, requester, day, start, end, origin, dest, purpose, passengers):
    page.select_option("#RequesterId", str(requester))
    page.fill("#Day", day.isoformat())
    page.fill("#From", start)
    page.fill("#To", end)
    page.fill("#Origin", origin)
    page.fill("#Destination", dest)
    page.fill("#Purpose", purpose)
    page.fill("#Passengers", str(passengers))


def fill_car(page, plate, make, model, kind, seats, year, state="Operational"):
    page.fill("#Plate", plate)
    page.fill("#Make", make)
    page.fill("#Model", model)
    page.select_option("#Kind", kind)
    page.fill("#PassengerSeats", str(seats))
    page.fill("#ReleaseYear", str(year))
    page.select_option("#State", state)


def wait_options(page):
    page.wait_for_selector("#options form.choice, #options .flash", timeout=5000)


# ---------------------------------------------------------------- справочники

@check("С01", "Справочники", "Поиск автомобиля по-русски в нижнем регистре",
       "строка поиска «газ»", "Автомобили → ввести «газ» → Показать",
       "найден один автомобиль К408СУ 186 (ГАЗ Газель Next)")
def c01(page):
    page.goto(f"{BASE}/Cars")
    page.fill("input[name=q]", "газ")
    page.click("form.filters button")
    page.wait_for_load_state("networkidle")
    plates = page.locator("table.grid .plate").all_inner_texts()
    return f"найдено: {', '.join(plates) or 'ничего'}", plates == ["К408СУ 186"]


@check("С02", "Справочники", "Отбор автомобилей по состоянию",
       "состояние «на ремонте»", "Автомобили → выбрать «на ремонте» → Показать",
       "в списке только Н903ЕВ 86 (Ford Transit)")
def c02(page):
    page.goto(f"{BASE}/Cars")
    page.select_option("select[name=state]", "InRepair")
    page.click("form.filters button")
    page.wait_for_load_state("networkidle")
    plates = page.locator("table.grid .plate").all_inner_texts()
    return f"найдено: {', '.join(plates)}", plates == ["Н903ЕВ 86"]


@check("С03", "Справочники", "Добавление автомобиля",
       "у777хх 86, Lada Largus, микроавтобус, 6 мест, 2024 г.",
       "Автомобили → Добавить автомобиль → заполнить → Сохранить",
       "карточка автомобиля У777ХХ 86 (номер приведён к заглавным буквам), сообщение «добавлен»")
def c03(page):
    page.goto(f"{BASE}/Cars/Create")
    fill_car(page, "у777хх 86", "Lada", "Largus", "Minibus", 6, 2024)
    submit(page)
    title = page.locator("h1").inner_text()
    return f"{title.strip()}; {flash(page)}", "У777ХХ 86" in title and "добавлен" in flash(page)


@check("С04", "Справочники", "Повторный госномер",
       "А214КМ186 (уже есть в справочнике, без пробела)", "Добавить автомобиль → ввести номер → Сохранить",
       "отказ: «Автомобиль с госномером А214КМ 186 уже есть в справочнике»")
def c04(page):
    page.goto(f"{BASE}/Cars/Create")
    fill_car(page, "А214КМ186", "LADA", "Vesta", "Sedan", 4, 2021)
    submit(page)
    e = errors(page)
    return "; ".join(e) or "сохранено", any("уже есть" in x for x in e)


@check("С05", "Справочники", "Неверные данные автомобиля",
       "госномер AB123, марка пустая, мест 0, год 1980", "Добавить автомобиль → заполнить → Сохранить",
       "четыре сообщения на русском у полей, автомобиль не сохранён")
def c05(page):
    page.goto(f"{BASE}/Cars/Create")
    fill_car(page, "AB123", "", "Vesta", "Sedan", 0, 1980)
    submit(page)
    e = errors(page)
    return "; ".join(e), len(e) == 4 and page.url.endswith("/Cars/Create")


@check("С06", "Справочники", "Госномер, набранный латинскими буквами",
       "A777MP 186 (буквы A, M, P латинские)", "Добавить автомобиль → заполнить → Сохранить",
       "номер принят и сохранён кириллицей: А777МР 186")
def c06(page):
    page.goto(f"{BASE}/Cars/Create")
    fill_car(page, "A777MP 186", "UAZ", "Patriot", "Sedan", 4, 2020)
    submit(page)
    title = page.locator("h1").inner_text()
    ok = "А777МР 186" in title
    return (title.strip() if ok else "; ".join(errors(page))), ok


@check("С07", "Справочники", "Нечисловое значение в числовом поле",
       "мест: «abc», год: пусто", "Добавить автомобиль → заполнить → Сохранить",
       "сообщения у полей на русском языке, без английского текста; автомобиль не сохранён")
def c07(page):
    page.goto(f"{BASE}/Cars/Create")
    fill_car(page, "М555ММ 86", "UAZ", "Patriot", "Sedan", 4, 2020)
    page.evaluate("""() => { const s = document.querySelector('#PassengerSeats'); s.type = 'text'; s.value = 'abc';
                             document.querySelector('#ReleaseYear').value = ''; }""")
    submit(page)
    e = errors(page)
    return "; ".join(e), len(e) >= 2 and all(not any(w in x for w in ("value", "invalid")) for x in e)


@check("С08", "Справочники", "Сотрудник: добавление и повторный табельный номер",
       "12001, Орлов Сергей Ильич; затем 10231 (занят)", "Сотрудники → Добавить → Сохранить; повторить с 10231",
       "первый сохранён; второй отказ «Сотрудник с таким табельным номером уже есть»")
def c08(page):
    page.goto(f"{BASE}/Employees/Create")
    for f, v in (("#PersonnelNo", "12001"), ("#FullName", "Орлов Сергей Ильич"), ("#Department", "ИТ-отдел"), ("#Position", "техник")):
        page.fill(f, v)
    submit(page)
    first = flash(page)
    page.goto(f"{BASE}/Employees/Create")
    for f, v in (("#PersonnelNo", "10231"), ("#FullName", "Тестов Тест"), ("#Department", "Бухгалтерия"), ("#Position", "кассир")):
        page.fill(f, v)
    submit(page)
    e = errors(page)
    return f"{first}; {'; '.join(e)}", "добавлен" in first and any("уже есть" in x for x in e)


@check("С09", "Справочники", "Несуществующая запись",
       "адрес /Cars/Card/999", "открыть адрес в браузере",
       "страница «Запись не найдена», код ответа 404")
def c09(page):
    r = page.goto(f"{BASE}/Cars/Card/999")
    return f"код {r.status}, «{page.locator('h1').inner_text()}»", r.status == 404 and "не найдена" in page.locator("h1").inner_text()


# ---------------------------------------------------------------- заявки

@check("З01", "Заявки", "Подача заявки",
       f"Лебедев И. О., {TOMORROW:%d.%m.%Y} 14:00–15:30, ул. Ленина, 41 → «Сургутнефтегаз», цель «Совещание по договору», 2 пассажира",
       "Заявки → Новая заявка → заполнить → Подать заявку",
       "заявка ЗТ-0011 в статусе «новая», в журнале «Заявка создана»")
def z01(page):
    page.goto(f"{BASE}/Trips/Create")
    fill_trip(page, 3, TOMORROW, "14:00", "15:30", "ул. Ленина, 41", "ПАО «Сургутнефтегаз», ул. Кукуевицкого, 1", "Совещание по договору", 2)
    submit(page)
    h = page.locator("h1").inner_text()
    log = page.locator(".log li").all_inner_texts()
    return f"{h.strip()}; журнал: {log[-1].strip() if log else '—'}", "ЗТ-0011" in h and "новая" in h and any("создана" in x for x in log)


@check("З02", "Заявки", "Заявка на прошедшее время",
       f"{YESTERDAY:%d.%m.%Y} 14:00–15:00", "Новая заявка → заполнить → Подать",
       "отказ «Нельзя оформить поездку на прошедшее время»")
def z02(page):
    page.goto(f"{BASE}/Trips/Create")
    fill_trip(page, 3, YESTERDAY, "14:00", "15:00", "ул. Ленина, 41", "Налоговая", "Сдача документов", 1)
    submit(page)
    e = errors(page)
    return "; ".join(e), any("прошедшее" in x for x in e)


@check("З03", "Заявки", "Несколько ошибок в заявке",
       "выезд 16:00, возвращение 15:00; «Склад» → «склад»; пассажиров 0", "Новая заявка → заполнить → Подать",
       "три сообщения: время окончания, совпадающие пункты, число пассажиров")
def z03(page):
    page.goto(f"{BASE}/Trips/Create")
    fill_trip(page, 3, TOMORROW, "16:00", "15:00", "Склад", "склад", "Перевозка образцов", 0)
    submit(page)
    e = errors(page)
    return "; ".join(e), len(e) == 3


@check("З04", "Заявки", "Граница длительности поездки",
       "06:00–20:00 (14 часов)", "Новая заявка → заполнить → Подать",
       "отказ «Поездка не может длиться дольше 12 часов»")
def z04(page):
    page.goto(f"{BASE}/Trips/Create")
    fill_trip(page, 6, TOMORROW, "06:00", "20:00", "ул. Ленина, 41", "г. Нефтеюганск", "Объезд филиалов", 2)
    submit(page)
    e = errors(page)
    return "; ".join(e), any("12 часов" in x for x in e)


@check("З05", "Заявки", "Заявка от уволенного сотрудника",
       "Белоусов В. Г. (уволен) подставлен в форму в обход списка", "Новая заявка → подменить заявителя → Подать",
       "отказ «… уволен и не может подавать заявки»")
def z05(page):
    page.goto(f"{BASE}/Trips/Create")
    fill_trip(page, 3, TOMORROW, "09:00", "10:00", "ул. Ленина, 41", "Банк", "Документы", 1)
    page.evaluate("() => { const o = document.createElement('option'); o.value = '10'; document.querySelector('#RequesterId').append(o); document.querySelector('#RequesterId').value = '10'; }")
    submit(page)
    e = errors(page)
    return "; ".join(e), any("уволен" in x for x in e)


@check("З06", "Заявки", "Изменение новой заявки",
       "ЗТ-0006: возвращение 12:30 → 12:45", "Карточка ЗТ-0006 → Изменить → время → Сохранить",
       "сохранено; в журнале «Изменены данные заявки»; время 10:30–12:45")
def z06(page):
    page.goto(f"{BASE}/Trips/Edit/6")
    page.fill("#To", "12:45")
    submit(page)
    facts = page.locator(".facts").inner_text()
    log = page.locator(".log li").all_inner_texts()
    return f"{flash(page)}; {'12:45' in facts and 'время 10:30–12:45' or facts[:60]}", "12:45" in facts and any("Изменены" in x for x in log)


@check("З07", "Заявки", "Изменение назначенной заявки по прямому адресу",
       "ЗТ-0004 (автомобиль назначен), адрес /Trips/Edit/4", "открыть адрес",
       "форма не открывается: переход в карточку с сообщением, что изменяются только новые заявки")
def z07(page):
    page.goto(f"{BASE}/Trips/Edit/4")
    h = page.locator("h1").inner_text()
    ok = "Изменение" not in h and "только новые" in flash(page)
    return (f"открыта карточка: {flash(page)}" if ok else f"открыта форма «{h.strip()}»"), ok


# ---------------------------------------------------------------- назначение и занятость

@check("Н01", "Назначение", "Подбор автомобиля к заявке",
       "ЗТ-0006, завтра 10:30–12:45, 2 пассажира", "открыть карточку ЗТ-0006, дождаться списка автомобилей",
       "А214КМ 186 недоступен «занят: ЗТ-0005 10:00–11:00», Е771ТН 86 — «занят: ЗТ-0004», Н903ЕВ 86 — «на ремонте»")
def n01(page):
    page.goto(f"{BASE}/Trips/Card/6")
    wait_options(page)
    off = [t.strip() for t in page.locator(".opt.off").all_inner_texts()]
    joined = " / ".join(" ".join(x.split()) for x in off)
    ok = any("А214КМ" in x and "ЗТ-0005" in x for x in off) and any("Е771ТН" in x and "ЗТ-0004" in x for x in off) and any("ремонте" in x for x in off)
    return joined, ok


@check("Н02", "Назначение", "Назначение занятого автомобиля в обход списка",
       "ЗТ-0006 и А214КМ 186 (занят ЗТ-0005 10:00–11:00)", "разблокировать вариант в форме → Назначить",
       "отказ «Автомобиль А214КМ 186 занят в это время: заявка ЗТ-0005…», статус «новая»")
def n02(page):
    page.goto(f"{BASE}/Trips/Card/6")
    wait_options(page)
    page.evaluate("() => { const r = document.querySelector('input[name=carId][value=\"1\"]'); r.disabled = false; r.checked = true; }")
    page.click("#options button")
    page.wait_for_load_state("networkidle")
    f = flash(page)
    return f, "занят" in f and "ЗТ-0005" in f and "новая" in page.locator("h1").inner_text()


@check("Н03", "Назначение", "Успешное назначение автомобиля",
       "ЗТ-0006 и В530ОР 186 (Toyota Camry, свободен)", "карточка ЗТ-0006 → выбрать В530ОР 186 → Назначить",
       "статус «автомобиль назначен», в журнале «Назначен автомобиль В530ОР 186»")
def n03(page):
    page.goto(f"{BASE}/Trips/Card/6")
    wait_options(page)
    page.check("input[name=carId][value='2']")
    page.click("#options button")
    page.wait_for_load_state("networkidle")
    h = page.locator("h1").inner_text()
    log = page.locator(".log li").all_inner_texts()
    return f"{h.strip()}; {log[-1].strip()}", "назначен" in h and "В530ОР" in log[-1]


@check("Н04", "Назначение", "Вместимость: делегация из 12 человек",
       "ЗТ-0007, 12 пассажиров", "карточка ЗТ-0007 → список автомобилей → назначить К408СУ 186",
       "легковые и семиместные недоступны («мест 4, а пассажиров 12»), Газель назначается")
def n04(page):
    page.goto(f"{BASE}/Trips/Card/7")
    wait_options(page)
    fits = [" ".join(t.split()) for t in page.locator(".opt:not(.off)").all_inner_texts()]
    small = page.locator(".opt.off", has_text="мест 4, а пассажиров 12").count()
    page.click("#options button")
    page.wait_for_load_state("networkidle")
    h = page.locator("h1").inner_text()
    return f"подходят: {'; '.join(fits)}; назначено: {'да' if 'назначен' in h else 'нет'}", len(fits) == 1 and "К408СУ" in fits[0] and small >= 3 and "назначен" in h


@check("Н05", "Назначение", "Проверка «Свободен ли автомобиль?»",
       f"А214КМ 186, {TOMORROW:%d.%m.%Y}: 10:30–11:30, затем 11:00–12:00", "карточка автомобиля → дата и время → Проверить (дважды)",
       "сначала «Занят … ЗТ-0005», затем «Свободен» (поездка встык)")
def n05(page):
    page.goto(f"{BASE}/Cars/Card/1")
    page.fill("aside input[name=day]", TOMORROW.isoformat())
    page.fill("aside input[name=from]", "10:30")
    page.fill("aside input[name=to]", "11:30")
    page.click("aside button")
    page.wait_for_selector("#busy .flash")
    first = " ".join(page.locator("#busy").inner_text().split())
    page.fill("aside input[name=from]", "11:00")
    page.fill("aside input[name=to]", "12:00")
    page.click("aside button")
    page.wait_for_function("() => document.querySelector('#busy').innerText.includes('Свободен')", timeout=3000)
    second = " ".join(page.locator("#busy").inner_text().split())
    return f"{first} / {second}", "Занят" in first and "ЗТ-0005" in first and "Свободен" in second


@check("Н06", "Назначение", "График занятости на завтра",
       f"дата {TOMORROW:%d.%m.%Y}", "Занятость → выбрать дату",
       "полосы ЗТ-0004 (Е771ТН 86), ЗТ-0005 (А214КМ 186), ЗТ-0006 (В530ОР 186); Н903ЕВ 86 отмечен «на ремонте»")
def n06(page):
    page.goto(f"{BASE}/Board?day={TOMORROW.isoformat()}")
    slots = page.locator(".slot").all_inner_texts()
    ok = all(any(n in s for s in slots) for n in ("ЗТ-0004", "ЗТ-0005", "ЗТ-0006")) and page.locator(".repair-note").count() == 1
    return "полосы: " + ", ".join(s.strip() for s in slots), ok


@check("Н07", "Назначение", "Ремонт автомобиля с закреплённой поездкой",
       "Е771ТН 86 закреплён за ЗТ-0004; состояние → «на ремонте»", "Автомобиль → Изменить → Состояние → Сохранить",
       "отказ «За автомобилем закреплены поездки: ЗТ-0004…»")
def n07(page):
    page.goto(f"{BASE}/Cars/Edit/3")
    page.select_option("#State", "InRepair")
    submit(page)
    e = errors(page)
    return "; ".join(e) or flash(page), any("ЗТ-0004" in x for x in e)


@check("Н08", "Назначение", "Уменьшение числа мест у закреплённого автомобиля",
       "К408СУ 186 закреплён за ЗТ-0007 (12 пассажиров); мест 15 → 4", "Автомобиль → Изменить → Пассажирских мест → Сохранить",
       "отказ: в закреплённой поездке ЗТ-0007 едут 12 человек")
def n08(page):
    page.goto(f"{BASE}/Cars/Edit/4")
    page.fill("#PassengerSeats", "4")
    submit(page)
    e = errors(page)
    if e:
        return "; ".join(e), any("ЗТ-0007" in x for x in e)
    seats = page.locator(".facts dd").nth(1).inner_text()
    return f"сохранено, мест: {seats}; ЗТ-0007 с 12 пассажирами осталась за автомобилем", False


# ---------------------------------------------------------------- статусы

@check("Т01", "Статусы", "Выезд раньше времени",
       f"ЗТ-0004 начинается {TOMORROW:%d.%m.%Y} в 09:00", "карточка ЗТ-0004 → Отметить выезд",
       "отказ «Выезд можно отметить не раньше чем за час до начала поездки»")
def t01(page):
    page.goto(f"{BASE}/Trips/Card/4")
    page.click("text=Отметить выезд")
    page.wait_for_load_state("networkidle")
    f = flash(page)
    return f, "не раньше чем за час" in f


@check("Т02", "Статусы", "Завершение поездки",
       "ЗТ-0009 в пути", "карточка ЗТ-0009 → Завершить поездку",
       "статус «выполнена», запись в журнале")
def t02(page):
    page.goto(f"{BASE}/Trips/Card/9")
    page.click("text=Завершить поездку")
    page.wait_for_load_state("networkidle")
    h = page.locator("h1").inner_text()
    return f"{h.strip()}; {flash(page)}", "выполнена" in h


@check("Т03", "Статусы", "Отклонение заявки: без причины и с причиной",
       "ЗТ-0010; причина «нет», затем «Встреча перенесена на следующую неделю»", "карточка → причина → Отклонить (дважды)",
       "сначала отказ «Укажите причину…», затем статус «отклонена», причина в карточке")
def t03(page):
    page.goto(f"{BASE}/Trips/Card/10")
    page.fill("input[name=reason]", "нет")
    page.click("text=Отклонить")
    page.wait_for_load_state("networkidle")
    first = flash(page)
    page.fill("input[name=reason]", "Встреча перенесена на следующую неделю")
    page.click("text=Отклонить")
    page.wait_for_load_state("networkidle")
    h = page.locator("h1").inner_text()
    facts = page.locator(".facts").inner_text()
    return f"{first} / {h.strip()}", "причину" in first and "отклонена" in h and "перенесена" in facts


@check("Т04", "Статусы", "Отмена назначенной заявки освобождает автомобиль",
       "ЗТ-0005 (А214КМ 186, завтра 10:00–11:00), причина «Документы отправлены курьером»",
       "карточка → причина → Отменить заявку; затем проверить занятость А214КМ 186 на 10:00–11:00",
       "статус «отменена»; А214КМ 186 свободен")
def t04(page):
    page.goto(f"{BASE}/Trips/Card/5")
    page.fill("input[name=reason]", "Документы отправлены курьером")
    page.click("text=Отменить заявку")
    page.wait_for_load_state("networkidle")
    h = page.locator("h1").inner_text()
    answer = json.loads(urllib.request.urlopen(f"{BASE}/api/cars/1/busy?from={TOMORROW}T10:00&to={TOMORROW}T11:00").read())
    return f"{h.strip()}; занят: {answer['busy']}", "отменена" in h and answer["busy"] is False


@check("Т05", "Статусы", "Снятие автомобиля с заявки",
       "ЗТ-0006 (В530ОР 186)", "карточка ЗТ-0006 → Снять автомобиль",
       "статус «новая», автомобиль не назначен, запись «снят с заявки»")
def t05(page):
    page.goto(f"{BASE}/Trips/Card/6")
    page.click("text=Снять автомобиль")
    page.wait_for_load_state("networkidle")
    h = page.locator("h1").inner_text()
    log = page.locator(".log li").all_inner_texts()
    return f"{h.strip()}; {log[-1].strip()}", "новая" in h and "снят" in log[-1]


# ---------------------------------------------------------------- поиск и хранение

@check("П01", "Поиск", "Отбор заявок без перезагрузки страницы",
       "поиск «суд», статус «новая»", "Заявки → ввести слово → выбрать статус",
       "таблица обновилась без перезагрузки, в ней ЗТ-0006")
def p01(page):
    page.goto(f"{BASE}/Trips")
    page.evaluate("() => window.__marker = 1")
    page.fill("input[name=Text]", "суд")
    page.select_option("select[name=Status]", "New")
    page.wait_for_function("() => document.querySelectorAll('#trip-rows tbody tr').length === 1", timeout=5000)
    rows = page.locator("#trip-rows tbody tr").all_inner_texts()
    same_page = page.evaluate("() => window.__marker === 1")
    return f"строк: {len(rows)}; {rows[0].split()[0]}; без перезагрузки: {same_page}", same_page and "ЗТ-0006" in rows[0]


@check("П02", "Поиск", "JSON API: занятость автомобиля",
       f"/api/cars/3/busy?from={TOMORROW}T10:00&to={TOMORROW}T11:00", "запрос в браузере",
       "{\"busy\": true, …} с заявкой ЗТ-0004")
def p02(page):
    page.goto(f"{BASE}/api/cars/3/busy?from={TOMORROW}T10:00&to={TOMORROW}T11:00")
    body = json.loads(page.locator("body").inner_text())
    return f"busy={body['busy']}, заявки: {[c['number'] for c in body['conflicts']]}", body["busy"] and body["conflicts"][0]["number"] == "ЗТ-0004"


@check("П03", "Хранение", "Данные после перезапуска приложения",
       "добавленные в С03 автомобиль У777ХХ 86 и в З01 заявка ЗТ-0011", "остановить приложение (Ctrl+C), запустить снова, открыть списки",
       "автомобиль и заявка на месте, статусы сохранены")
def p03(page):
    restart = SETTINGS.get("restart")
    if restart:
        restart()
    page.goto(f"{BASE}/Cars")
    has_car = "У777ХХ 86" in page.locator("table.grid").inner_text()
    page.goto(f"{BASE}/Trips/Card/11")
    h = page.locator("h1").inner_text()
    return f"У777ХХ 86: {'есть' if has_car else 'нет'}; {h.strip()}", has_car and "ЗТ-0011" in h


SETTINGS = {}


def main():
    if "--restart" in sys.argv:
        command = sys.argv[sys.argv.index("--restart") + 1]

        def restart():
            # останавливается только сервер: «dotnet run …» и его исполняемый файл
            subprocess.run(["pkill", "-f", r"^(dotnet run |\S*/TransportOrders\.Web$)"], check=False)
            time.sleep(2)
            subprocess.Popen(command, shell=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
            for _ in range(60):
                try:
                    urllib.request.urlopen(BASE, timeout=1)
                    return
                except Exception:
                    time.sleep(1)
        SETTINGS["restart"] = restart

    with sync_playwright() as p:
        browser = p.chromium.launch(args=["--lang=ru-RU"])
        page = browser.new_page(viewport={"width": 1280, "height": 860}, locale="ru-RU")
        for fn in CHECKS:
            m = fn.meta
            try:
                actual, passed = fn(page)
            except Exception as ex:  # сбой самой проверки тоже фиксируется
                actual, passed = f"ошибка проверки: {type(ex).__name__}: {str(ex).splitlines()[0]}", False
            shot = f"{m['code']}.png"
            page.screenshot(path=str(SHOTS / shot), full_page=True)
            results.append({**m, "actual": actual, "passed": passed, "shot": shot})
            print(f"{m['code']} {'пройдена ' if passed else 'НЕ ПРОЙДЕНА'}  {m['title']}: {actual}")
        browser.close()

    (OUT / "results.json").write_text(json.dumps(results, ensure_ascii=False, indent=2), encoding="utf-8")
    failed = [r["code"] for r in results if not r["passed"]]
    print(f"\nИтого: {len(results) - len(failed)} из {len(results)}; не пройдены: {', '.join(failed) or 'нет'}")


if __name__ == "__main__":
    main()
