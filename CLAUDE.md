# CardThrow — контекст проекта для Claude (единый источник правды)

> Этот файл загружается в «Знания проекта» (Project knowledge) у каждого участника и лежит в корне репозитория как `CLAUDE.md`.
> Если запрос противоречит этому файлу — Claude должен указать на противоречие и предложить вариант, а не молча делать по-своему.
> Владелец файла — Данила (тимлид). Изменения контрактов (раздел 5) — только через него.

---

## 0. Как Claude должен работать с нами

- Отвечай по-русски. Код — C# для **Unity 6.6 (6000.6.5f1)**, 2D (URP 2D template).
- Пиши **полные файлы целиком** (не фрагменты «...остальное без изменений»), каждый файл в отдельном блоке с путём `Assets/_Project/...`.
- После кода всегда: (1) «Как подключить в Unity» — пошагово, какие компоненты на какие объекты, какие поля заполнить; (2) «Почему так» — 3–6 пунктов о ключевых решениях. На защите жюри задаёт технические вопросы: автор должен понимать каждую строку.
- Не меняй публичные контракты из раздела 5 (имена классов, публичные методы, события, enum). Если без изменения не обойтись — явно напиши «НУЖНО СОГЛАСОВАТЬ С ДАНИЛОЙ: …» и предложи минимальную правку.
- Не добавляй сторонние пакеты/ассеты (DOTween, Cinemachine и т.п.) — только встроенное в Unity 6.6. Анимации — корутины + `AnimationCurve`.
- Предпочитай простое и объяснимое решение «умному». Критерий жюри: «простая и понятная архитектура».

---

## 1. Джем: жёсткие требования (FunFlow Game Jam × ТПУ)

- Движок: **Unity 6.6, версия 6000.6.5f1** — строго эта версия, иначе жюри может не открыть проект.
- Сдача: ссылка на **публичный** GitHub-репозиторий; проект скачивается и открывается в редакторе на **Windows и Mac**.
- Дедлайн: **13 октября до 13:00 (Томск)**. Наш внутренний срок сдачи — **11:30**.
- Игру не допускают к оценке только если её не смогли запустить → запуск важнее любой фичи.
- Правила и цель понятны за **30 секунд**; сессия **2–5 минут**; понятные победа/поражение; **быстрый рестарт**; сложность **растёт**; на каждое действие игрока — **визуальная и/или звуковая реакция**.
- Вариант «Катапульта» (референс Angry Birds), минимальный объём: управление направлением и силой выстрела; физика полёта и столкновений; разрушаемые или перемещаемые конструкции; очки или ограничение попыток; несколько коротких уровней или один с усложнением. Если останется время: разные снаряды, материалы с разной прочностью, цепные реакции и бонусы за красивое разрушение.
- Критерии оценки: качество основной механики; геймплей и интерес; управление и понятность; game feel и обратная связь; визуальное оформление (целостность и читаемость, а не сложность графики); техническое качество (простая архитектура, стабильность, корректная физика, производительность, Restart).
- «Дополнительные функции не компенсируют слабую базовую механику». Лучше одна отполированная механика.
- ИИ разрешён, но «сгенерировал — разберись»: на финале будут технические вопросы.
- **Жюри запускает игру в редакторе Unity, а не из .exe.** Скачивают репозиторий (ZIP или git clone) → Unity Hub → Add → открывают проект версией 6000.6.5f1 → открыта сцена `Main` → кнопка ▶ Play вверху редактора → игра идёт в окне Game внутри редактора. Билды (.exe / .app) **не делаем и не кладём в репо**. Следствия:
  - Console видна жюри → **ноль красных ошибок**, минимум жёлтых предупреждений, без спама логами.
  - Окно Game может быть любого размера и пропорций (Free Aspect, маленькая панель) → `CameraFitter` + Canvas Scaler обязательны, проверяем на разных размерах.
  - Play должен запускать игру, даже если открыта другая сцена → `AutoOpenMainScene` открывает `Main` и назначает её стартовой сценой Play Mode.
  - Клавиатура работает, только когда окно Game в фокусе → любой экран можно пройти и мышью (кнопки), первый клик по окну Game не должен ничего ломать.
  - Жюри видит проект изнутри (папки, иерархию, инспектор, код) → чистые имена, никаких мусорных/пробных файлов в main, шаблонная `SampleScene` удалена.
- **Тест игры на защите — 5 минут** → игра = обучение (уровень 0) + 3 уровня с заметным ростом сложности.

---

## 2. Игра (краткий GDD)

**Название (рабочее):** CardThrow.
**Питч:** Ты — крупье-шулер. Пока игроки отвлеклись, незаметно «флик-броском» отправь через весь стол нужную карту своему напарнику, чтобы у него было ровно 21. Не задень бутылки и карточные домики, не попади к чужим игрокам — иначе тебя раскроют.

**Камера:** вид строго сверху, ортографическая; виден весь продолговатый стол. Крупье сидит у **левого узкого торца** стола, бросок идёт **вдоль всей длины** стола слева направо (как в Angry Birds). Напарник — у правого торца, соперники — вдоль верхнего и нижнего краёв по бокам от линии броска.

**Напарник:** подсвечен золотым контуром, над головой — его счёт (например «14»). У остальных игроков тоже есть счёт над головой.

### Управление (всегда показано на экране)

| Действие | Основное | Дублирование |
|---|---|---|
| Выбрать карту из трёх | A / D | ← / → |
| Прицелиться и задать силу | Зажать ЛКМ и тянуть **назад** (как рогатка) | — |
| Бросок | Отпустить ЛКМ | — |
| Закрут (−3…+3) | Колёсико мыши (1 щелчок = 1 шаг) | Q (влево) / E (вправо) |
| Отменить прицел | ПКМ или Esc | — |
| Рестарт забега | R | Кнопка на экране |

Положительный закрут (+) = карта уходит дугой **влево**, отрицательный (−) = **вправо**.

### Ход уровня (раздачи)

1. Баннер «Уровень 2/3 — название» (≈1.2 с), игроки отворачиваются (головы поворачиваются от стола).
2. Запускается таймер «окно внимания» (по умолчанию 20 с, задаётся в уровне). За 3 с до конца головы начинают поворачиваться обратно + тиканье.
3. Игрок выбирает одну из 3 карт (A/D), тянет мышь назад — видит точки траектории (только часть пути!), колёсиком задаёт закрут, отпускает.
4. Во время полёта таймер стоит. Карта скользит по сукну, тормозит трением, может уйти дугой, отскочить от стакана.
5. Итог (см. таблицу). После неудачи уровень перезапускается (препятствия восстанавливаются, выбранная карта и закрут сохраняются), подозрение +1.

### Обучение — уровень 0 (`Hand_00`, `LevelConfig.IsTutorial = true`)

Цель — за **~30 секунд** научить трём главным действиям так, чтобы жюри не читало правил. **Таймера нет, подозрение не растёт, очки не начисляются.** Стол: напарник на правом торце прямо напротив крупье (большая зона), бутылка шампанского на прямой линии между ними (чуть смещена, обойти можно закрутом), один соперник далеко сбоку, у верхнего края (его зона подсвечена красным). Превью — 100%.

Шаги ведёт `TutorialController`: подсказка крупно над столом + иконка управления + стрелка-указатель. Следующий шаг открывается **действием игрока**, а не кнопкой «Далее».

| Шаг | Подсказка (финальные тексты — Егор, `UiTexts`) | Иконка / указатель | Переход |
|---|---|---|---|
| 1 | «Напарник в золоте. У него 14 — дай ему 21: выбери карту клавишами A / D» | клавиши A, D; стрелка на напарника | выбрана правильная карта |
| 2 | «Бутылку задевать нельзя. Закрути карту: колесо мыши или Q / E» | колесо мыши, Q, E; стрелка на бутылку | `spinStep ≠ 0` |
| 3 | «Зажми ЛКМ и тяни назад, как рогатку. Точки — путь карты. Отпусти!» | анимированная мышь «оттяг» | бросок (`CardThrown`) |
| ✓ | «Отлично! Дальше — на время, пока соперники отвернулись» | — | через 2 с → уровень 1 |
| ✗ | Подсказка по причине: недолёт — «Тяни сильнее», вылет со стола — «Слишком сильно», бутылка — «Закрути сильнее», красная зона — «Чужие карты трогать нельзя!», не та карта — «Нужна карта, дающая ровно 21» | — | переигровка без штрафа, сразу шаг 3 (карта и закрут сохранены) |

**Tab** — пропустить обучение (подсказка «Tab — пропустить» в углу). Если обучение уже пройдено в текущем запуске Play, то `StartRun()`/R начинают сразу с уровня 1.

### Исходы броска

| Исход (`ThrowOutcome`) | Условие | Последствие |
|---|---|---|
| `Success` | Карта остановилась центром в зоне карт напарника и `partnerScore + cardValue == 21` | «BLACKJACK!», очки, следующая раздача |
| `WrongValue` | Остановилась в зоне напарника, но сумма ≠ 21 | «Перебор!» / «Недобор!», подозрение +1, переигровка |
| `HitBreakable` | Задела хрупкий предмет (бутылка, карточный домик…) | Предмет эффектно разрушается, карта мнётся и падает, игроки возмущаются, подозрение +1 |
| `OpponentCaught` | Центр карты влетел в зону карт соперника | «Шулер!» — **мгновенное поражение** |
| `OffTable` | Карта вылетела за край стола | Карта падает на пол, подозрение +1 |
| `StoppedOnTable` | Остановилась на столе вне зон | Подозрение +1 |
| `TimeOut` | Таймер закончился до броска | «Вас заметили!», подозрение +1 |

**Подозрение:** шкала из 3 делений (иконки «глаз»). 3/3 → поражение «Вас вычислили». Подозрение сохраняется между раздачами.

**Победа:** пройдены уровни 1–3 → экран «Смена окончена», итоговый выигрыш, рекорд.

### Очки (за `Success`)
- База 1000 + 50 × целые секунды, оставшиеся на таймере.
- Точность: до +500 (`500 × zoneAccuracy01`, 1 = центр зоны).
- Трюки: +300 за каждый рикошет, +200 если бросок с закрутом (`spinStep != 0`), +250 за золотую фишку (если сделаем).
- Рекорд в `PlayerPrefs` (ключ `CT_HighScore`).

### Объекты на столе

| Объект | Тип (`ObstacleKind`) | Поведение |
|---|---|---|
| Гранёный стакан | `Ricochet` | Неразрушим, карта отскакивает (коэф. 0.85), закрут ×0.5 |
| Бутылка шампанского | `Breakable` | Разбивается: осколки + пена, провал броска |
| Карточный домик | `Breakable` | Эффектно складывается, провал броска |
| Стопка фишек | `Pushable` | Разлетается (физика), карта теряет 35% скорости и летит дальше; штрафа нет. Фишка, врезавшаяся в хрупкий предмет, разбивает его → провал (цепная реакция) |
| Поднос официанта (движется) | `Breakable` + `ObstacleMover` | Движущееся препятствие на уровне 3 |
| Золотая фишка (опц.) | триггер-бонус | Подбирается картой, +250 |

### Уровни (0–3; точные координаты — у Егора)

| № | Название | Что нового / в чём сложность | Время | Превью | Зона напарника | На столе |
|---|---|---|---|---|---|---|
| 0 | Обучение | Выбор карты, закрут, бросок — по шагам | нет | 100% | 1.6×1.1 | 1 бутылка, 1 соперник далеко |
| 1 | Первая сдача | Таймер и подозрение «включаются»; близкие номиналы карт; прямой путь закрыт бутылкой, обход закрутом | 25 с | 100% | 1.4×1.0 | бутылка, карточный домик, стопка фишек, 2 соперника |
| 2 | Рикошет | Напарник за стеной из карточных домиков: отскок от стакана **или** сильный закрут | 20 с | 70% | 1.2×0.85 | стакан, 2 домика, бутылка, фишки, 3 соперника |
| 3 | Большой куш | Напарник между двумя соперниками, движущийся поднос, нужна комбинация закрута и рикошета | 15 с | 45% | 0.9×0.7 | стакан, поднос (движется), 2 бутылки, фишки, 4 соперника |

Рост сложности **виден без пояснений**: меньше времени, короче точки превью, меньше зона, больше препятствий, соперники ближе к напарнику, движение.
Хронометраж: обучение ≈ 30–40 с, уровни ≈ 20–40 с каждый с анимациями; с 1–2 переигровками сессия ≈ 2–3 минуты — укладывается и в требование «2–5 минут», и в 5-минутный тест на защите.
Ручки сложности в `LevelConfig`: `TimeLimit`, `PreviewFraction`, размер зоны напарника (BoxCollider2D зоны), число и расстановка препятствий, близость соперников.

---

## 3. Модель полёта (точная, одна для броска и для превью)

Карта — не Rigidbody-«мяч», а **кинематический объект с собственным детерминированным интегратором**. Причина: превью траектории обязано совпадать с реальным полётом; встроенный физдвижок это не гарантирует. Один и тот же статический метод `FlightSimulator.Step` используют и `CardFlight` (в `FixedUpdate`), и `TrajectoryPreview` (в цикле). Шаг `dt = Time.fixedDeltaTime = 0.02`.

```
Состояние: pos (Vector2), vel (Vector2), spin (рад/с), time
Запуск:    vel  = direction * power01 * maxSpeed     // броски с power01 < minPower AimController отменяет
           spin = spinStep * curvePerStep
Шаг(dt):   speed = |vel|;  если speed <= stopSpeed → vel = 0, карта остановилась
           dir   = vel / speed
           dir   = Rotate(dir, spin * dt)                         // закрут: поворот вектора скорости
           speed = max(0, speed - (linearDrag * speed + friction) * dt)  // сопротивление + трение сукна
           vel   = dir * speed
           pos  += vel * dt
           spin *= max(0, 1 - spinDecay * dt)
           time += dt
```

**Столкновения (каждый шаг, до перемещения):** `Physics2D.CircleCast(pos, cardRadius, dir, speed*dt, obstacleMask)`.
- `Ricochet`: `pos = hit.centroid + hit.normal * 0.02f`; `vel = Vector2.Reflect(vel, hit.normal) * ricochetRestitution`; `spin *= 0.5f`; `ricochets++`.
- `Pushable`: `obstacle.OnCardHit(point, vel)`; `vel *= (1 - pushSpeedLoss)`; этот коллайдер игнорируется 0.2 с.
- `Breakable`: `obstacle.OnCardHit(...)` → полёт заканчивается, `FlightResult.HitBreakable`.
- Внимание: `Physics2D.queriesStartInColliders` по умолчанию true — поэтому после отскока сдвигаем карту по нормали.

**Зоны:** каждый шаг `Physics2D.OverlapPoint(pos, zoneFilter)` (ContactFilter2D с `useTriggers = true`, слой Zone). Зона соперника → `EnteredOpponentZone` сразу. Зона напарника проверяется **только в момент остановки**.
**Стол:** если `pos` вне триггера `TableSurface` (слой Table) → `OffTable`.

**Общий шаг с коллизиями:** физика столкновения (отражение, остановка шага) вынесена в статический `FlightSimulator.StepWithCollisions(...)` — его вызывают и `CardFlight`, и `TrajectoryPreview`. Побочные эффекты (сломать предмет, толкнуть фишку, звук, события) выполняет только `CardFlight`. Так превью гарантированно совпадает с полётом.

**Превью:** симулируем весь путь до остановки (макс. `maxPreviewSteps`), считаем полную длину L, рисуем точки до длины `L * previewFraction`. На `Ricochet` — отражаем и продолжаем; на `Breakable`/`Pushable` — ставим маркер «✕» и обрываем. Превью только читает физику, ничего не ломает.

**Ощущение высоты (game feel):** `h = speed / maxSpeed`; масштаб карты `1 + 0.25*h`; тень смещена на `(0.15, -0.15) * h`; вращение спрайта пропорционально скорости и закруту.

### FlightSettings (ScriptableObject `Data/FlightSettings.asset`), стартовые значения
| Поле | Значение | Смысл |
|---|---|---|
| maxSpeed | 21 | ед/с при силе 100% |
| minPower | 0.12 | минимальная доля силы (меньше — бросок отменяется) |
| linearDrag | 0.8 | 1/с |
| friction | 2.5 | ед/с² |
| stopSpeed | 0.15 | ед/с |
| curvePerStep | 0.35 | рад/с на 1 шаг закрута (шаги −3…+3) |
| spinDecay | 0.3 | 1/с |
| cardRadius | 0.25 | ед. |
| ricochetRestitution | 0.85 | — |
| pushSpeedLoss | 0.35 | — |
| maxDragDistance | 2.0 | ед. мира — оттяг мыши для 100% силы |
| maxPreviewSteps | 400 | — |

При этих числах бросок на 100% проходит ≈18 ед. и длится ≈2.5 с (70% ≈ 11.5 ед., 45% ≈ 6.4 ед.); закрут 3 поворачивает траекторию примерно на 105°, закрут 1 — на ~35°. До зоны напарника ≈ 14 ед., поэтому нужна сила около 78%. Балансирует Егор.

---

## 4. Мир, сцена, слои

- Камера: ортографическая, size 6 (видно 12 ед. по высоте), `CameraFitter` подгоняет size так, чтобы стол целиком помещался при любом соотношении сторон (16:9, 16:10 на Mac, «Free Aspect» в окне Game).
- Центр стола = (0, 0). Стол ≈ 19 × 9.5 ед. (x ∈ [−9.5; 9.5], y ∈ [−4.75; 4.75]). Крупье в (−9, 0) у левого торца, точка броска (−7.6, 0), бросок направлен вдоль +X. Напарник у правого торца в (7.9, 0), его зона карт — в (6.4, 0). Соперники сидят у верхнего (y ≈ 3.7, зона в y ≈ 2.4) и нижнего (y ≈ −3.7, зона в y ≈ −2.4) краёв. Расстояние от точки броска до зоны напарника ≈ 14 ед.; «тянуть назад» = тянуть мышь влево.
- Слои (настраивает Данила при создании проекта): 6 `Card`, 7 `Obstacle`, 8 `Zone`, 9 `Table`. Матрица коллизий 2D: Obstacle↔Obstacle включено (цепные реакции фишек).
- Спрайты: **PPU = 200** (рисуем в 2×). Сортировка: Sorting Layers `Table`, `Objects`, `Card`, `FX` (порядок снизу вверх).
- UI: uGUI + TextMeshPro, Canvas Screen Space Overlay, Canvas Scaler «Scale With Screen Size» 1920×1080, Match 0.5. Шрифт с кириллицей (Montserrat, Dynamic SDF).
- Ввод: **Input System (новый)**, опрос устройств напрямую: `Mouse.current`, `Keyboard.current` (`using UnityEngine.InputSystem;`).

### Иерархия `Main.unity` (единственная игровая сцена, владелец — Данила)
```
[Systems]        GameManager, LevelManager, TutorialController, AudioManager, SoundEventsBridge
Main Camera      Camera(ortho), CameraFitter, CameraShake
Table            SpriteRenderer + TableSurface (BoxCollider2D isTrigger, слой Table)
Dealer           [префаб Prefabs/Gameplay/Dealer] DealerHand (3 карты), AimController, LaunchPoint (child)
Card             [префаб Prefabs/Gameplay/Card] CardFlight, SpriteRenderer, Shadow (child), слой Card. БЕЗ Rigidbody2D и коллайдера: двигаем transform в FixedUpdate, столкновения — CircleCast
TrajectoryPreview [префаб Prefabs/Gameplay/TrajectoryPreview]
FeedbackManager  [префаб Prefabs/Gameplay/FeedbackManager]
LevelRoot        сюда инстанциируется префаб текущего уровня Hand_00 (обучение) … Hand_03
UI               Canvas: HUD, TitleScreen, ResultPopup, GameOverScreen, VictoryScreen
EventSystem      InputSystemUIInputModule
```

Объекты Алексея (Dealer, Card, TrajectoryPreview, FeedbackManager) лежат в сцене **как экземпляры префабов**: Алексей настраивает свои компоненты в префабах и не трогает `Main.unity`. В сцене Данила один раз проставляет только связи между объектами.

### Префаб раздачи `Prefabs/Levels/Hand_0N.prefab` (владелец — Егор)
```
Hand_03 (LevelConfig)
├─ Seats   Seat_Partner (Seat, IsPartner=true) └─ Zone (ZoneType.Partner, BoxCollider2D trigger)
│          Seat_Opp_1..n (Seat)               └─ Zone (ZoneType.Opponent)
└─ Obstacles  Glass, Bottle, CardHouse, Chips… (префабы из Prefabs/Obstacles)
```

### Структура папок
```
Assets/_Project/
  Art/Sprites/{Table,Cards,Characters,Obstacles,UI,VFX}   Art/Fonts
  Audio/{SFX,Music}
  Data/            FlightSettings.asset, GameSettings.asset, SoundLibrary.asset
  Prefabs/{Gameplay,Obstacles,Seats,Levels,UI,VFX}
  Scenes/          Main.unity, Sandbox/Sandbox_Alexey.unity
  Scripts/{Core,Gameplay,Obstacles,UI,Feedback,Audio,Editor}
```

---

## 5. Архитектура и контракты кода

Принципы: (1) системы общаются через статические C#-события `GameEvents`, а не прямыми ссылками друг на друга; (2) настройки — в ScriptableObject, никаких «магических чисел» в коде; (3) одна игровая сцена, уровни — префабы; (4) `GameManager` — единственный, кто решает исход и меняет состояние игры; (5) пространство имён `CardThrow`, один класс = один файл, имя файла = имя класса.

### 5.1 Общие типы — `Scripts/Core/GameTypes.cs` (пишет Данила первым делом)
```csharp
using UnityEngine;

namespace CardThrow
{
    public enum GameState { Title, HandIntro, Aiming, Flying, Resolving, HandResult, GameOver, Victory }

    // Физический итог полёта (решает CardFlight)
    public enum FlightResult { LandedInPartnerZone, HitBreakable, EnteredOpponentZone, OffTable, StoppedOnTable }

    // Игровой итог броска (решает GameManager)
    public enum ThrowOutcome { Success, WrongValue, HitBreakable, OpponentCaught, OffTable, StoppedOnTable, TimeOut }

    public enum ObstacleKind { Ricochet, Breakable, Pushable }
    public enum ZoneType { Partner, Opponent }

    public struct ThrowParams
    {
        public Vector2 origin;
        public Vector2 direction;   // нормализован
        public float power01;       // 0..1
        public int spinStep;        // -3..3, + = дуга влево
        public int cardValue;       // номинал брошенной карты
    }

    public struct FlightReport
    {
        public FlightResult result;
        public Vector2 finalPosition;
        public int ricochets;
        public int spinStep;
        public int cardValue;
        public float zoneAccuracy01; // 1 = центр зоны напарника, 0 = край (только для LandedInPartnerZone)
        public int bonusPickups;
    }
}
```

### 5.2 События — `Scripts/Core/GameEvents.cs` (Данила)
```csharp
using System;
using UnityEngine;

namespace CardThrow
{
    public static class GameEvents
    {
        public static event Action<GameState> StateChanged;            // GameManager
        public static event Action<int, int, string> HandStarted;      // GameManager: levelNumber (0 = обучение), totalLevels (3), title
        public static event Action<string> HintChanged;                // GameManager / TutorialController: подсказка над столом ("" = скрыть)
        public static event Action<float, float> TimerChanged;         // GameManager: remaining, total
        public static event Action<int, int> SuspicionChanged;         // GameManager: current, max
        public static event Action<int> ScoreChanged;                  // GameManager: total score
        public static event Action<int, int> CardSelected;             // DealerHand: index, value
        public static event Action<int> SpinChanged;                   // AimController: spinStep
        public static event Action<float> AimChanged;                  // AimController: power01 (0 = прицел снят)
        public static event Action<ThrowParams> CardThrown;            // AimController
        public static event Action<Vector2, ObstacleKind> CardHitObstacle; // CardFlight: point, kind
        public static event Action<FlightReport> CardFlightFinished;   // CardFlight
        public static event Action<ThrowOutcome, int> ThrowResolved;   // GameManager: outcome, scoreGained

        public static void RaiseStateChanged(GameState s) => StateChanged?.Invoke(s);
        public static void RaiseHandStarted(int i, int total, string title) => HandStarted?.Invoke(i, total, title);
        public static void RaiseHintChanged(string text) => HintChanged?.Invoke(text);
        public static void RaiseTimerChanged(float rem, float total) => TimerChanged?.Invoke(rem, total);
        public static void RaiseSuspicionChanged(int cur, int max) => SuspicionChanged?.Invoke(cur, max);
        public static void RaiseScoreChanged(int score) => ScoreChanged?.Invoke(score);
        public static void RaiseCardSelected(int index, int value) => CardSelected?.Invoke(index, value);
        public static void RaiseSpinChanged(int step) => SpinChanged?.Invoke(step);
        public static void RaiseAimChanged(float p) => AimChanged?.Invoke(p);
        public static void RaiseCardThrown(ThrowParams p) => CardThrown?.Invoke(p);
        public static void RaiseCardHitObstacle(Vector2 pt, ObstacleKind k) => CardHitObstacle?.Invoke(pt, k);
        public static void RaiseCardFlightFinished(FlightReport r) => CardFlightFinished?.Invoke(r);
        public static void RaiseThrowResolved(ThrowOutcome o, int gained) => ThrowResolved?.Invoke(o, gained);

        // Сброс подписок при входе в Play Mode (на случай отключённого Domain Reload)
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetAll()
        {
            StateChanged = null; HandStarted = null; HintChanged = null; TimerChanged = null; SuspicionChanged = null;
            ScoreChanged = null; CardSelected = null; SpinChanged = null; AimChanged = null;
            CardThrown = null; CardHitObstacle = null; CardFlightFinished = null; ThrowResolved = null;
        }
    }
}
```
Правило: подписка в `OnEnable`, отписка в `OnDisable`.

### 5.2a Заглушки (contract-first)
На вехе M0 Данила кладёт в репозиторий **заглушки всех классов из таблицы 5.3** — с точными публичными сигнатурами и пустыми телами (стартовый пакет `CardThrow_starter_scripts`). Поэтому проект компилируется с первой минуты, и каждый может вызывать чужой API, даже если тот ещё не реализован.
Владелец класса **заменяет тело заглушки, сохраняя публичный API**. Добавлять новые публичные члены можно; переименовывать и удалять существующие — только через Данилу.
`FlightState` (struct) объявлен в `Scripts/Gameplay/FlightSimulator.cs` (владелец — Алексей).

### 5.3 Классы, владельцы и публичные API

| Класс (файл) | Папка | Владелец | Публичный API / ответственность |
|---|---|---|---|
| `GameTypes`, `GameEvents` | Core | Данила | см. выше |
| `GameManager` | Core | Данила | `static Instance`; `GameState State`; `LevelConfig CurrentLevel`; `bool IsTutorial`; `StartRun()`; `RestartRun()`; `SkipTutorial()`; `DebugLoadHand(int)`. Машина состояний, таймер, подозрение, очки, перевод `FlightReport` → `ThrowOutcome` |
| `LevelManager` | Core | Данила | `int HandCount`; `LevelConfig Current`; `LevelConfig LoadHand(int index)` (уничтожает старый инстанс, создаёт новый под LevelRoot) |
| `LevelConfig` | Core | Данила (данные — Егор) | свойства только для чтения: `IsTutorial`, `HandTitle`, `Hint`, `PartnerSeat`, `PartnerStartScore`, `int[] CardValues` (ровно 3), `TimeLimit`, `PreviewFraction`, `CorrectCardIndex`; `OnValidate` предупреждает, если правильных карт ≠ 1 |
| `TutorialController` | Core | Данила (тексты — Егор в `UiTexts`) | работает только на уровне 0: слушает `HandStarted`, `CardSelected`, `SpinChanged`, `CardThrown`, `ThrowResolved`, ведёт шаги, поднимает `HintChanged`, показывает иконки и стрелку-указатель; провал → подсказка по причине |
| `GameSettings` (SO) | Core | Данила | `maxSuspicion=3`, длительности баннеров/попапов, константы очков |
| `Seat` | Core | Данила | `string DisplayName`; `bool IsPartner`; `int Score`; `SetScore(int)`; `SetLookingAway(bool)` и `SetLookingAway(bool, float turnDuration)` (поворот головы); `Zone Zone` |
| `CameraFitter`, `SaveSystem` (static) | Core | Данила | вписывание стола в экран; `SaveSystem.HighScore`, `SaveSystem.TrySetHighScore(int)` |
| `HUD`, `TitleScreen`, `ResultPopup`, `EndScreen` | UI | Данила | только слушают события, вызывают `GameManager.StartRun/RestartRun` |
| `UiTexts` (SO, `Data/UiTexts.asset`) | UI | Данила (тексты — Егор) | все тексты интерфейса: правила, подсказки управления, шаги обучения и подсказки при провале в обучении, варианты заголовков для каждого `ThrowOutcome`, экраны конца |
| `SoundEventsBridge` | Audio | Данила | слушает `GameEvents` и играет общие звуки (выбор карты, бросок, стакан, исходы, таймер). Звуки разрушения играют сами препятствия (`[SerializeField] SoundId breakSound`) |
| `AudioManager`, `SoundLibrary` (SO), `SoundId` (enum) | Audio | Данила (наполняет Артас) | статические `AudioManager.Play(SoundId)`, `PlayLoop(SoundId)`, `StopLoop(SoundId)`; случайный вариант клипа и pitch ±5% |
| `AutoOpenMainScene` | Editor | Данила | при открытии проекта с пустой сценой открывает `Main.unity` |
| `FlightSettings` (SO), `FlightState` (struct), `FlightSimulator` (static) | Gameplay | Алексей | `FlightState Launch(ThrowParams, FlightSettings)`; `FlightState Step(FlightState, FlightSettings, float dt)`; чистые функции без коллизий. В `FlightSettings` также маски слоёв `obstacleMask`, `zoneMask`, `tableMask` и `maxSpinStep = 3` |
| `CardFlight` | Gameplay | Алексей | `Launch(ThrowParams)`; `ResetCard()`; `bool IsFlying`. Полёт в `FixedUpdate`, коллизии/зоны, визуал высоты; поднимает `CardHitObstacle`, `CardFlightFinished` |
| `AimController` | Gameplay | Алексей | `SetEnabled(bool)`; `float PreviewFraction { set; }`. Ввод мыши/колеса/Q/E, поднимает `SpinChanged`, `AimChanged`, `CardThrown`, вызывает `CardFlight.Launch` |
| `DealerHand` | Gameplay | Алексей | `SetCards(int[] values, int selectedIndex = -1)` (−1 = средняя карта; при переигровке GameManager передаёт прежний индекс); `int SelectedIndex`; `int SelectedValue`; `SetInteractable(bool)`; A/D/←/→; поднимает `CardSelected` |
| `TrajectoryPreview` | Gameplay | Алексей | `Show(ThrowParams p, float fraction)`; `Hide()`; пул точек-спрайтов |
| `Obstacle` (abstract) + `RicochetObstacle`, `BreakableObstacle`, `PushableObstacle`, `ObstacleMover` | Obstacles | Алексей | `ObstacleKind Kind`; `virtual void OnCardHit(Vector2 point, Vector2 cardVelocity)`; Breakable также ломается от удара фишки (`OnCollisionEnter2D`, порог скорости) и сообщает об этом |
| `Zone` | Obstacles | Алексей | `ZoneType Type`; `Seat Seat`; `Vector2 Center`; `float Radius` |
| `FeedbackManager`, `CameraShake`, `PopupText` | Feedback | Алексей | слушают события: тряска, hit-stop, slow-mo на успехе, всплывающие тексты, частицы |

**Цепная реакция:** `BreakableObstacle`, разбитый фишкой, сам вызывает `GameEvents.RaiseCardHitObstacle(point, ObstacleKind.Breakable)`. `CardFlight` подписан на это событие и, если карта ещё летит, завершает полёт с `HitBreakable` (повторные вызовы игнорируются флагом). Реализует Алексей.

### 5.4 Машина состояний `GameManager`
```
Title --StartRun()--> HandIntro --(1.2с)--> Aiming --CardThrown--> Flying --CardFlightFinished--> Resolving
Aiming --таймер=0--> Resolving(TimeOut)
Resolving --(Success)--> HandResult --> HandIntro(следующая) | Victory (если последняя)
Resolving --(провал, подозрение < max)--> HandResult --> HandIntro(та же раздача заново)
Resolving --(OpponentCaught или подозрение = max)--> GameOver
GameOver/Victory --R/кнопка--> StartRun()
R в любом состоянии --> RestartRun()
Tab на уровне 0 --> SkipTutorial() --> уровень 1
Shift+0..3 (только в редакторе; полезно и жюри) --> DebugLoadHand(n)

Обучение (CurrentLevel.IsTutorial): таймера нет (HUD прячет полосу), подозрение и очки не меняются.
  Провал --> HandResult (подсказка по причине) --> тот же уровень 0, выбранная карта и закрут сохраняются.
  Успех  --> HandResult («Отлично!») --> уровень 1; флаг tutorialDone = true (живёт до выхода из Play).
StartRun(): tutorialDone ? уровень 1 : уровень 0. Нумерация в HUD: «Обучение», «Уровень 1/3» … «Уровень 3/3».
LevelManager.hands = [Hand_00, Hand_01, Hand_02, Hand_03]; totalLevels = HandCount − 1.
```
Порядок вызовов при старте уровня: `LevelManager.LoadHand(i)` → `DealerHand.SetCards(level.CardValues, retry ? prevIndex : -1)` → `CardFlight.ResetCard()` → `AimController.PreviewFraction = level.PreviewFraction` → `RaiseHandStarted` → `RaiseHintChanged(level.Hint)` → `HandIntro` → `AimController.SetEnabled(true)` при входе в `Aiming`.
Во время slow-mo `Time.timeScale < 1` → таймеры UI и попапов считаются по `Time.unscaledDeltaTime`.

### 5.5 SoundId (enum в `Scripts/Audio/SoundId.cs`)
`UiClick, CardSelect, CardPinch, CardThrow, CardLand, GlassClink, BottleBreak, CardHouseCollapse, ChipsScatter, CardCrumple, CrowdGasp, CrowdAngry, Caught, Success, CoinRain, TimerTick, TimerWarning, GameOver, Victory, MusicLoop, AmbienceLoop`

---

## 6. Правила кода (Unity 6.6)

- `[SerializeField] private` вместо public-полей. Публичное — только API из таблицы 5.3. Исключение: в ScriptableObject-настройках (`FlightSettings`, `GameSettings`, `SoundLibrary`) поля public — это чистые данные.
- API Unity 6: `Rigidbody2D.linearVelocity` (не `velocity`), `linearDamping` (не `drag`), `FindFirstObjectByType`/`FindAnyObjectByType` (не `FindObjectOfType`). Без устаревших предупреждений.
- Никаких `Find`/`GetComponent` в `Update`; ссылки — через инспектор или кэш в `Awake`.
- Физика карты — в `FixedUpdate`; ввод — в `Update`.
- Скрипты из папки `Editor` — только `UnityEditor`-код; в игровых скриптах `using UnityEditor` только внутри `#if UNITY_EDITOR`.
- Никаких asmdef, никаких внешних пакетов, никакого async/await — корутины.
- Без `Debug.Log` в каждом кадре; логи с префиксом `[CT]`.
- Ошибка компиляции в main = стоп-кран для всей команды. Перед push — проект компилируется, Console без красных ошибок, Play стартует.

---

## 7. Git и владение файлами

- **Билды (.exe/.app) не делаем**: жюри открывает проект в редакторе. Папку `Build/` не коммитим.
- Репозиторий публичный, ветка `main`. **Без Git LFS** (жюри может скачать ZIP — LFS-файлы там превращаются в пустышки). Ассеты делаем лёгкими: PNG, OGG/WAV короткие.
- `.gitignore` — шаблон Unity (Library/, Temp/, Obj/, Build/, Logs/, UserSettings/ не коммитим). Коммитим `Assets/`, `Packages/`, `ProjectSettings/` и все `.meta`.
- Клиент: GitHub Desktop (или Claude Code / git CLI у программистов).
- Ритм: **Pull → работа 30–60 мин → проверка (компилируется, Play работает) → Commit → Pull → Push**. Маленькие коммиты с понятным сообщением: `[Алексей] Рикошет от стакана`.
- **Владение (чужое не трогаем без «замка» в чате `#locks`: «беру Bottle.prefab» → «отдал»):**

| Путь | Владелец |
|---|---|
| `Scenes/Main.unity`, `ProjectSettings/`, `Packages/`, `CLAUDE.md` | Данила |
| `Scripts/Core`, `Scripts/UI`, `Scripts/Audio`, `Scripts/Editor`, `Prefabs/UI`, `Prefabs/Seats` | Данила |
| `Scripts/Gameplay`, `Scripts/Obstacles`, `Scripts/Feedback`, `Prefabs/Gameplay`, `Prefabs/Obstacles`, `Prefabs/VFX`, `Scenes/Sandbox/` | Алексей |
| `Prefabs/Levels/`, значения в `Data/FlightSettings.asset`, `Data/GameSettings.asset`, `Data/UiTexts.asset` | Егор |
| `Art/` | Кирилл |
| `Audio/`, `Data/SoundLibrary.asset`, `README.md`, `.github/` | Артас |

- **Трюк с плейсхолдерами:** все спрайты уже лежат в `Art/Sprites/...` под финальными именами (цветные заглушки). Кирилл **заменяет PNG-файл с тем же именем и тем же размером в пикселях** через Finder/Проводник. `.meta` (GUID и настройки импорта) остаётся → все префабы и сцена обновляются автоматически, без правок префабов и без конфликтов.
- Конфликт в `.unity`/`.prefab` не мёрджим руками: берём версию владельца («Discard my changes» / `git checkout --theirs`) и повторяем свои правки.

---

## 8. Арт-спецификация (кратко; полная — в документе Кирилла)

PPU 200, пивот Center, Filter Bilinear, Compression None (для UI и персонажей) / Normal Quality (для стола). Спрайты рисуются в 2×.

| Файл | px | ед. мира |
|---|---|---|
| `Table/table_felt.png` | 3800×1900 | 19×9.5 |
| `Table/floor_tile.png` | 512×512 | тайл |
| `Cards/card_back.png`, `card_2.png` … `card_11.png`, `card_crumpled.png` | 140×200 | 0.7×1.0 |
| `Cards/card_shadow.png` | 160×220 | — |
| `Characters/dealer_hand_idle.png`, `_pinch.png`, `_pull.png` | 400×240 | 2×1.2 |
| `Characters/player_body_1..4.png` | 320×240 | 1.6×1.2 |
| `Characters/player_head_1..4.png` | 140×140 | 0.7 |
| `Characters/partner_glow.png` | 360×280 | — |
| `Table/cards_pile.png`, `zone_partner.png`, `zone_opponent.png` | 280×200 | 1.4×1.0 |
| `Obstacles/glass.png` | 120×120 | 0.6 |
| `Obstacles/bottle.png`, `bottle_broken.png` | 140×140 | 0.7 |
| `Obstacles/cardhouse.png`, `cardhouse_broken.png` | 240×160 | 1.2×0.8 |
| `Obstacles/chip_red/blue/green/black/gold.png` | 90×90 | 0.45 |
| `Obstacles/waiter_tray.png` | 300×200 | 1.5×1.0 |
| `VFX/fx_shard, fx_foam, fx_sparkle, fx_coin, fx_dust, fx_star .png` | 64×64 | — |
| `VFX/fx_dot.png` (точка превью), `VFX/fx_cross.png` (маркер «удар») | 32×32 | — |
| `UI/ui_panel.png` (9-slice), `ui_button.png` (9-slice) | 128×128 | — |
| `UI/ui_eye_open.png`, `ui_eye_closed.png`, `ui_spin_arrow.png` | 128×128 | — |
| `UI/ui_timer_frame.png`, `ui_timer_fill.png` | 800×48 | — |
| `UI/logo.png` | 1200×400 | — |
| `UI/tut_arrow.png` (стрелка-указатель обучения) | 128×128 | — |
| `UI/icon_key_a/d/q/e/tab.png` (клавиши), `UI/icon_mouse_drag.png`, `UI/icon_mouse_wheel.png` | 96×96 / 128×128 | — |

**Читаемость (правило):** хрупкое — тёплые/красноватые блики; стекло-рикошет — холодный голубой блик; фишки — насыщенные цвета; напарник — золото; зоны соперников во время прицеливания слегка подсвечены красным.

---

## 9. Глоссарий
Уровень = раздача (Hand); уровень 0 = обучение. Крупье = игрок (Dealer). Напарник = Partner. Соперник = Opponent. Подозрение = жизни наоборот (Suspicion). Закрут = Spin. Превью = точки траектории (Trajectory preview). Окно внимания = таймер раздачи.
