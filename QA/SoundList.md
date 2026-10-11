# CardThrow — звуковой лист (T3)

Автор: Артас · версия 1 от 11.10 · SoundId — из `CLAUDE.md`, раздел 5.5
Сроки: базовые звуки к **12.10 13:00** (M2), всё остальное к **12.10 19:00** (M3).

## 0. Правила лицензий (строго)

| Источник | Можно? | Условие |
|---|---|---|
| **kenney.nl** | ✅ лучший вариант | Всё под CC0. Атрибуция не нужна, но в кредиты пишем |
| **freesound.org** | ✅ только CC0 | Фильтр License → «Creative Commons 0», **плюс** проверить лицензию на странице самого звука (справа). CC BY — только если CC0 не нашлось, с атрибуцией. **CC BY-NC и Sampling+ — нельзя.** Для скачивания нужен бесплатный аккаунт |
| **opengameart.org** | ✅ CC0, ⚠ CC BY | Лицензия — на странице трека. CC BY-SA и GPL не берём |
| **incompetech.com** (Kevin MacLeod) | ⚠ CC BY 4.0 | Можно только с атрибуцией в README: «"Название" Kevin MacLeod (incompetech.com), Licensed under Creative Commons: By Attribution 4.0» |
| **pixabay.com** | ⚠ только в крайнем случае | Лицензия Pixabay запрещает распространять файлы «как есть». Наш репозиторий публичный, и сырые `.ogg` в нём может скачать кто угодно — это серая зона. Берём только если больше нигде нет, и предупреждаем Данилу |
| YouTube Audio Library, Zapsplat, Mixkit, звуки из других игр, «бесплатные» сайты без лицензии | ❌ | Запрещают выкладывать сырые файлы или лицензия непонятна |

Каждый файл сразу записываем в таблицу кредитов (раздел 6). Нет записи — нет файла в репо.

## 1. Где что брать

**Паки Kenney (CC0)** — скачать сначала, они закрывают около 60% списка:
- [Casino Audio](https://kenney.nl/assets/casino-audio) — 50 звуков: карты (скольжение, шлепок, веер, тасовка), фишки, кости
- [Impact Sounds](https://kenney.nl/assets/impact-sounds) — 130 ударов: стекло, тарелки, дерево, металл
- [Interface Sounds](https://kenney.nl/assets/interface-sounds) и [UI Audio](https://kenney.nl/assets/ui-audio) — клики, тики, ошибки, подтверждения
- [Music Jingles](https://kenney.nl/assets/music-jingles) — 85 коротких джинглов; группа **SAX** подходит под казино
- [RPG Audio](https://kenney.nl/assets/rpg-audio) — звон монет (для `CoinRain`)

На kenney.nl: «Download» → «Continue without donating…». Названия файлов внутри паков на сайте не показаны — сверим после скачивания.

**Музыка (CC0, всё проверено на страницах 11.10):**

| Трек | Автор | Почему подходит | Формат |
|---|---|---|---|
| [Shop Theme](https://opengameart.org/content/shop-theme) | Cleyton Kauffman | Боссанова в «лифтовом» стиле, уже залуплена — **кандидат №1** | OGG, MP3 |
| [Red Heels (piano ver)](https://opengameart.org/content/red-heels-piano-ver) | TAD | Джазовое пианино со скрипкой, «loopable» | WAV, MP3 |
| [Jazzy blues](https://opengameart.org/content/jazzy-blues) | LushoGames | Джаз-блюз для главного меню; про луп ничего не сказано | MP3 |
| [Bossa Nova](https://opengameart.org/content/bossa-nova) | Joth | Запасной вариант: 129 BPM, звучит в 8-битном стиле | MP3 |

Решаем на слух. Нужен лаунж без вокала, не громкий, не раздражающий за 5 минут повторов. Shop Theme просит указать автора («Music by Cleyton Kauffman — soundcloud.com/cleytonkauffman») — пишем в кредиты, хоть это и CC0.

**Freesound (CC0, найдено поиском 11.10):** перед скачиванием проверь лицензию на странице звука.

| Для | Кандидаты |
|---|---|
| BottleBreak | [vrodge — bottle break 2ft](https://freesound.org/people/vrodge/sounds/119525/) · [semccab — bottle break 1](https://freesound.org/people/semccab/sounds/154392/) · [qyxs — Glass Bottle Shatter](https://freesound.org/people/qyxs/sounds/273647/) |
| CrowdGasp | [ShangusBurger — Crowd Gasping In Surprise](https://freesound.org/people/ShangusBurger/sounds/764102/) · [HowardV — small crowd gasp](https://freesound.org/people/HowardV/sounds/264376/) (длинный, вырезать 1 «ах») |
| CardCrumple | [drooler — crumple-05](https://freesound.org/people/drooler/sounds/508598/) · [WasabiWielder — Paper Crumple 3](https://freesound.org/people/WasabiWielder/sounds/334200/) |
| CardHouseCollapse | [Hano_van_Dalen — Cards Dropping](https://freesound.org/people/Hano_van_Dalen/sounds/767310/) (можно наложить на веер карт из Casino Audio) |
| AmbienceLoop | [craigsmith — Casino Ambience](https://freesound.org/people/craigsmith/sounds/438129/) · [mhtaylor67 — Casino Floor (mono)](https://freesound.org/people/mhtaylor67/sounds/277444/). Осторожно: в казино-записях бывают слот-машины, а у нас карточный стол. Если звенит — искать «restaurant murmur» |

## 2. Звуковой лист

Колонка «Кто играет»: **Bridge** — общий `SoundEventsBridge` (Данила), **Препятствие** — сам объект (`breakSound` в префабе Алексея). Громкость — значение `volume` в SoundLibrary (0…1), стартовое; сводим на слух в M3.

| SoundId | Когда звучит | Характер, длина | Где искать (запросы на английском) | Громк. | Pitch ± | Приор. |
|---|---|---|---|---|---|---|
| `UiClick` | Кнопки интерфейса | Мягкий короткий клик, 0.05–0.15 с | Kenney UI Audio / Interface Sounds: `click`, `select` · fs: `soft ui click` | 0.5 | 0.05 | M2 |
| `CardSelect` | A/D — выбор карты | Сухой шорох карты, 0.1–0.2 с | Casino Audio: `card-slide`, `card-place` · fs: `card flick`, `playing card slide short` | 0.5 | 0.08 | M2 |
| `CardPinch` | Начало оттяга (зажал ЛКМ) | Карта сгибается в пальцах, тихий «щелчок», 0.1–0.3 с | Casino Audio: `card-fan` (коротко) · fs: `card bend`, `paper flex` | 0.45 | 0.05 | M3 |
| `CardThrow` | Отпустил ЛКМ — бросок | Резкий «свист» карты, 0.2–0.4 с | Casino Audio: `card-shove`, `card-slide` · fs: `card whoosh`, `paper swish fast` | 0.8 | 0.07 | **M2** |
| `CardLand` | Карта остановилась | Мягкий шлепок по сукну, 0.1–0.3 с | Casino Audio: `card-place` · fs: `card drop on felt`, `card place table` | 0.6 | 0.06 | **M2** |
| `GlassClink` | Рикошет от стакана | Звонкий «дзынь» без разбивания, 0.3–0.8 с | Impact Sounds: `impactGlass_light` · fs: `glass clink`, `glass tap ring` | 0.8 | 0.06 | **M2** |
| `BottleBreak` | Разбилась бутылка (препятствие) | Разбитое стекло + брызги, 0.8–1.5 с | fs: кандидаты выше; запросы `bottle smash`, `glass shatter short` | 1.0 | 0.05 | **M2** |
| `CardHouseCollapse` | Рухнул карточный домик (препятствие) | Каскад падающих карт, 0.6–1.2 с | fs: `cards dropping`, `cards falling pile`; Casino Audio: `card-shuffle` + `card-fan` вместе | 0.9 | 0.05 | M3 |
| `ChipsScatter` | Карта врезалась в фишки | Пластиковый перестук, 0.3–0.8 с | Casino Audio: `chips-collide`, `chips-stack` · fs: `poker chips scatter` | 0.8 | 0.08 | M3 |
| `CardCrumple` | Карта смялась (HitBreakable) | Короткое смятие бумаги, 0.2–0.5 с | fs: кандидаты выше; `paper crumple short` | 0.7 | 0.08 | M3 |
| `CrowdGasp` | Провал (бутылка, вылет) | Несколько человек «ах!», 0.6–1.5 с | fs: кандидаты выше; `small crowd gasp`, `group surprise oh` | 0.7 | 0.03 | M3 |
| `CrowdAngry` | Игроки возмущены (провал, подозрение +1) | Недовольный гул, «эй!», 0.8–1.5 с | fs: `crowd disapproval`, `crowd boo short`, `group grumble` | 0.75 | 0.03 | M3 |
| `Caught` | «Шулер!» — поражение от соперника | Резкий драматичный удар/стинг, 0.8–2 с | Music Jingles: `jingles_HIT`, `jingles_SAX` (минорный) · fs: `dramatic sting`, `busted stinger` | 1.0 | 0 | **M2** |
| `Success` | BLACKJACK — успешный бросок | Яркий мажорный джингл, 1–2 с | Music Jingles: `jingles_SAX`, `jingles_PIZZI` · fs: `jackpot win short` | 1.0 | 0 | **M2** |
| `CoinRain` | Дождь фишек после успеха | Звон монет/фишек, 1–1.5 с | RPG Audio: `handleCoins` · Casino Audio: `chips-handle` · fs: `coins pour` | 0.8 | 0.05 | M3 |
| `TimerTick` | Последние 3 с таймера | Чистый «тик» часов, ≤0.1 с | Interface Sounds: `tick` · fs: `clock tick single` | 0.55 | 0 | M2 |
| `TimerWarning` | Время вышло — «Вас заметили!» | Тревожный звук/свисток, 0.5–1 с | Interface Sounds: `error` · fs: `alarm short`, `whistle referee short` | 0.7 | 0 | M3 |
| `GameOver` | Экран поражения | Грустный джингл, 2–4 с | Music Jingles: `jingles_SAX` (нисходящий) | 0.9 | 0 | **M2** |
| `Victory` | Экран «Смена окончена» | Торжественный джингл, 2–4 с | Music Jingles: `jingles_SAX`, `jingles_STEEL` | 0.9 | 0 | **M2** |
| `MusicLoop` | Всегда, с титульного экрана | Лаунж/боссанова, без вокала, 1–2 мин, бесшовный луп | Треки из раздела 1 | 0.35 | 0 | M3 |
| `AmbienceLoop` | Всегда, под музыкой | Гул голосов в зале, без слот-машин, 30–60 с луп | fs: кандидаты выше; `restaurant murmur loop`, `bar crowd ambience` | 0.25 | 0 | M3 |

**M2 (база к 12.10 13:00):** UiClick, CardSelect, CardThrow, CardLand, GlassClink, BottleBreak, Caught, Success, TimerTick, GameOver, Victory — 11 звуков. Остальные 10 — к M3.

**Вопросы к Даниле** (влияют на подготовку файлов):
1. `TimerTick` играется по одному разу каждую секунду (`Play`) или петлёй (`PlayLoop`)? Если петлёй — файл должен быть **ровно 1.000 с** с тиком в самом начале.
2. Кто вызывает `PlayLoop(MusicLoop)` и `PlayLoop(AmbienceLoop)` — `SoundEventsBridge` при старте?
3. В объекте `[Systems] → AudioManager` поле `library` указывает на `Data/SoundLibrary.asset`?

## 3. Требования к файлам

| | SFX | Джинглы | Музыка, атмосфера |
|---|---|---|---|
| Формат | WAV 16 бит или OGG | OGG | **OGG** (не MP3: MP3 добавляет тишину в начало и ломает луп) |
| Каналы | моно | моно или стерео | стерео |
| Частота | 44 100 Гц | 44 100 Гц | 44 100 Гц |
| Длина | ≤ 2 с | ≤ 4 с | 1–2 мин / 30–60 с |
| Тишина в начале | 0 мс (обрезать) | 0 | 0 |
| Пик | −3 dB | −3 dB | −3 dB, громкость потом в SoundLibrary |

Имена: латиница, строчные, без пробелов: `Audio/SFX/sfx_card_throw_01.ogg`, `sfx_card_throw_02.ogg` (варианты одного звука — разные номера), `Audio/Music/music_lounge_loop.ogg`, `Audio/Music/amb_casino_loop.ogg`.
2–3 варианта на частые звуки (`CardSelect`, `CardThrow`, `CardLand`, `GlassClink`, `ChipsScatter`) — AudioManager сам выбирает случайный.

## 4. Обработка в Audacity

Скачать: [audacityteam.org](https://www.audacityteam.org/download/) (бесплатно).

**Один SFX:**
1. File → Open. Ctrl+1 — приблизить.
2. **Обрезать тишину в начале:** выделить мышью от начала до первого всплеска волны → Delete. В конце так же, оставив естественный «хвост».
3. **Убрать щелчок на конце:** выделить последние ~20 мс → Effect → Fading → **Fade Out**.
4. **В моно:** Tracks → Mix → **Mix Stereo Down to Mono** (если дорожка стерео).
5. **Нормализовать:** Ctrl+A → Effect → Volume and Compression → **Normalize** → «Normalize peak amplitude to» = **−3.0 dB**, галка «Remove DC offset» → Apply.
6. **Частота:** внизу слева (или Audio Setup → Audio Settings) Project Rate = 44100. Если у файла 48000: Tracks → Resample → 44100.
7. **Экспорт:** File → Export Audio → Format: **Ogg Vorbis**, Quality 6 (или WAV 16-bit PCM) → имя по правилам выше.

**Много SFX сразу (макрос):** Tools → Macro Manager → New → добавить команды: Mix Stereo Down to Mono, Normalize (−3 dB), Export as OGG → Apply to Files → выбрать файлы. Тишину в начале всё равно проверь глазами.

**Бесшовный луп музыки:**
1. Открыть трек; если он уже залуплен автором (Shop Theme, Red Heels), не обрезать его, только Normalize и экспорт в OGG.
2. Если луп свой: включить **Select → At Zero Crossings** (клавиша Z после выделения), выделить отрезок от сильной доли такта до сильной доли через 16–32 такта.
3. **Проверить стык:** включить петлю (кнопка Loop на панели воспроизведения или клавиша L) и нажать Space — выделение играет по кругу. Слышен щелчок или сбой ритма — сдвинуть края.
4. Edit → Remove Special → **Trim Audio** (оставить выделенное) → Normalize −3 dB → Export → Ogg Vorbis, Quality 5.

## 5. Импорт в Unity и SoundLibrary

**Импорт:**
1. Скопировать файлы в `Assets/_Project/Audio/SFX/` и `Assets/_Project/Audio/Music/` через Проводник. Unity сама создаст `.meta` — **их тоже коммитим**.
2. Выделить все SFX в окне Project → Inspector:
   - Force To Mono: ✅
   - Load Type: **Decompress On Load**
   - Compression Format: Vorbis, Quality 70
   - Preload Audio Data: ✅
   - → **Apply**
3. Музыку и атмосферу:
   - Force To Mono: ❌
   - Load Type: **Streaming**
   - Compression Format: Vorbis, Quality 60
   - → **Apply**

**SoundLibrary** (`Assets/_Project/Data/SoundLibrary.asset` — твой файл):
1. Если ассета нет: правый клик в папке `Data` → Create → **CardThrow → Sound Library** → имя `SoundLibrary`.
2. Выделить ассет → Inspector → **Entries** → «+».
3. Для каждой записи:
   - `Id` — выбрать SoundId из списка;
   - `Clips` — перетащить 1–3 клипа;
   - `Volume` и `Pitch Jitter` — из таблицы раздела 2.
4. Одна запись на один SoundId. Если записей с одинаковым Id две, сработает последняя.
5. Проверка: ▶ Play, пройти smoke-тест, отметить SND-01…06 в тест-плане. Если звука нет — сначала проверь, что у записи есть клип, потом пиши баг Даниле (Bridge) или Алексею (препятствия).
6. Коммит: `[Артас] Звуки M2: 11 SoundId + SoundLibrary`. В коммит входят `Audio/**` с `.meta` и `Data/SoundLibrary.asset`.

## 6. Таблица кредитов (вставляется в README)

| Файл | Источник | Автор | Лицензия | Ссылка |
|---|---|---|---|---|
| `sfx_card_throw_01.ogg` | Kenney — Casino Audio | Kenney | CC0 | https://kenney.nl/assets/casino-audio |
| `music_lounge_loop.ogg` | OpenGameArt — Shop Theme | Cleyton Kauffman | CC0 | https://opengameart.org/content/shop-theme |
| … | | | | |
