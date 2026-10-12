# Арт CardThrow — источники

Весь арт сделан командой для FunFlow Game Jam × ТПУ. Чужих ассетов, фотографий, персонажей и логотипов нет.

«ИИ-генерация» — картинку сгенерировал Кирилл в генераторе изображений по своему промпту, затем она обработана скриптами Python (вырезка фона, подгонка размера под коллайдер, цвет). Сервис для каждого файла: **TODO Кирилл — уточнить (Gemini / ChatGPT / Leonardo)**.

| Файл (`Art/Sprites/…`) | Источник | Автор | Лицензия |
|---|---|---|---|
| `Table/table_felt`, `Table/floor_tile`, `Table/zone_opponent`, `Table/zone_partner` | ИИ-генерация + скрипт (бесшовный тайл, эмблема «21») | Кирилл | собственный арт команды |
| `Characters/player_body_1–4`, `Characters/player_head_1–4`, `Characters/waitress` | ИИ-генерация + скрипт | Кирилл | собственный арт команды |
| `Characters/dealer_idle`, `dealer_pinch`, `dealer_pull` | скрипт Python на основе фигур игроков Кирилла | Данила (с помощью Claude) | собственный арт команды |
| `Characters/dealer_body`, `dealer_hand_idle/pinch/pull` | ИИ-генерация + скрипт / скрипт | Кирилл, Данила | собственный арт команды |
| `Characters/partner_glow` | скрипт Python | Данила (с помощью Claude) | собственный арт команды |
| `Cards/card_back`, `Cards/card_2–11` | ИИ-генерация + скрипт | Кирилл | собственный арт команды |
| `Cards/card_crumpled`, `Cards/card_shadow` | скрипт Python | Данила (с помощью Claude) | собственный арт команды |
| `Obstacles/glass`, `ashtray`, `bottle`, `bottle_broken`, `chip_red/blue/green/black/gold` | ИИ-генерация + скрипт | Кирилл | собственный арт команды |
| `Obstacles/cardhouse`, `Obstacles/cardhouse_broken` | скрипт Python из наших карт `card_back` / `card_7` (`ArtWork/_tools/make_cardhouse.py`) | Кирилл (с помощью Claude) | собственный арт команды |
| `UI/logo` | ИИ-генерация + скрипт | Кирилл | собственный арт команды |
| `UI/icon_key_a/d/q/e/tab` | ИИ-генерация + скрипт | Кирилл | собственный арт команды |
| `UI/icon_mouse_drag` | скрипт Python на основе сгенерированной клавиши | Кирилл (с помощью Claude) | собственный арт команды |
| `UI/tut_arrow` | скрипт Python, векторная форма (`ArtWork/_tools/make_tut_arrow.py`) | Кирилл (с помощью Claude) | собственный арт команды |
| `UI/ui_button`, `ui_frame_gold`, `ui_heart_full/empty`, `ui_spin_0–3`, `ui_timer_frame/fill`, `icon_mouse_wheel` | скрипт Python | Данила (с помощью Claude) | собственный арт команды |
| `VFX/fx_coin`, `fx_dust`, `fx_foam`, `fx_shard`, `fx_sparkle`, `fx_star` | скрипт Python | Данила (с помощью Claude) | собственный арт команды |
| `Background/decor_palm`, `Background/decor_lounge` | скрипт Python | Данила (с помощью Claude) | собственный арт команды |
| `UI/ui_panel`, `ui_eye_open/closed`, `ui_spin_arrow`, `VFX/fx_dot`, `fx_cross`, `Obstacles/waiter_tray`, `bottle-1`, `Table/cards_pile` | заглушки M0 (простые фигуры) | Данила | собственный арт команды |
