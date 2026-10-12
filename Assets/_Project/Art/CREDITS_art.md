# Арт CardThrow — источники

Весь арт сделан командой для FunFlow Game Jam × ТПУ. Чужих персонажей, фотографий и логотипов нет. Сторонний ассет — только шрифт Montserrat (OFL).

«ИИ-генерация» — картинку сгенерировал Кирилл в генераторе изображений по своему промпту, затем она обработана скриптами Python (вырезка фона, подгонка размера под коллайдер, цвет). Сервис: Google Gemini (Nano Banana Pro).

| Файл (`Art/Sprites/…`) | Источник | Автор | Лицензия |
|---|---|---|---|
| `Table/table_felt`, `Table/floor_tile`, `Table/zone_opponent`, `Table/zone_partner` | ИИ-генерация (Gemini) + скрипт (бесшовный тайл, эмблема «21») | Кирилл | собственный арт команды |
| `Characters/player_body_1–4`, `Characters/player_head_1–4`, `Characters/waitress` | ИИ-генерация (Gemini) + скрипт | Кирилл | собственный арт команды |
| `Characters/dealer_idle`, `dealer_pull` | скрипт Python на основе фигур игроков Кирилла | Данила (с помощью Claude) | собственный арт команды |
| `Characters/dealer_pinch` | скрипт Python из `dealer_idle`: предплечье повёрнуто в локте, рукав целый | Кирилл (с помощью Claude) | собственный арт команды |
| `Characters/hostess` | ИИ-генерация (Gemini) + скрипт (вырезка фона, правая кисть восстановлена зеркальной копией левой) | Кирилл | собственный арт команды |
| `Characters/partner_glow` | скрипт Python | Данила (с помощью Claude) | собственный арт команды |
| `Cards/card_back`, `Cards/card_2–11` | ИИ-генерация (Gemini) + скрипт | Кирилл | собственный арт команды |
| `Cards/card_crumpled` | скрипт Python | Данила (с помощью Claude) | собственный арт команды |
| `Obstacles/ashtray`, `bottle`, `bottle_broken`, `chip_red/blue/green/black` | ИИ-генерация (Gemini) + скрипт | Кирилл | собственный арт команды |
| `Obstacles/cardhouse`, `Obstacles/cardhouse_broken` | скрипт Python из наших карт `card_back` / `card_7` | Кирилл (с помощью Claude) | собственный арт команды |
| `UI/logo` | ИИ-генерация (Gemini) + скрипт | Кирилл | собственный арт команды |
| `UI/icon_key_a/d/q/e/tab` | ИИ-генерация (Gemini) + скрипт | Кирилл | собственный арт команды |
| `UI/icon_mouse_drag`, `UI/icon_mouse_wheel` | скрипт Python на основе сгенерированной клавиши | Кирилл (с помощью Claude) | собственный арт команды |
| `UI/ui_panel` | скрипт Python из `ui_frame_gold` (9-slice под границу 32) | Кирилл (с помощью Claude) | собственный арт команды |
| `UI/tut_arrow` | скрипт Python, векторная форма | Кирилл (с помощью Claude) | собственный арт команды |
| `UI/ui_button`, `ui_frame_gold`, `ui_heart_full/empty`, `ui_spin_0–3`, `ui_timer_fill` | скрипт Python | Данила (с помощью Claude) | собственный арт команды |
| `VFX/fx_coin`, `fx_dust`, `fx_foam`, `fx_shard`, `fx_sparkle`, `fx_star` | скрипт Python | Данила (с помощью Claude) | собственный арт команды |
| `VFX/fx_dot`, `VFX/fx_cross` | скрипт Python | Кирилл (с помощью Claude) | собственный арт команды |
| `Background/decor_palm`, `Background/decor_lounge` | скрипт Python | Данила (с помощью Claude) | собственный арт команды |
| `Obstacles/waiter_tray` | заглушка (простая фигура) | Данила | собственный арт команды |
| `Art/Fonts/Montserrat-Bold.ttf` (+ `Montserrat-Bold SDF.asset`) | Google Fonts — Montserrat | Julieta Ulanovsky и др. | SIL Open Font License 1.1 |
