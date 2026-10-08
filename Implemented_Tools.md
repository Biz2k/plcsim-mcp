# Инструменты MCP-сервера PLCSIM Advanced

Полный список имён инструментов — в `docs/tools-list.txt`.

## Жизненный цикл инстансов
- `plcsim_list_instances`: Список всех зарегистрированных виртуальных контроллеров.
- `plcsim_create_instance`: Создание нового виртуального контроллера с выбором типа CPU.
- `plcsim_delete_instance`: Безопасная остановка и удаление виртуального контроллера.
- `plcsim_get_instance_config`: Получить конфигурацию инстанса (Не реализовано).

## Управление состоянием
- `plcsim_power_on`: Включить питание виртуального ПЛК.
- `plcsim_power_off`: Выключить питание виртуального ПЛК.
- `plcsim_run`: Перевод ПЛК в режим выполнения программы.
- `plcsim_stop`: Перевод ПЛК в режим остановки.
- `plcsim_memory_reset`: Сброс памяти виртуального контроллера (MRES).
- `plcsim_get_instance_state`: Получить текущее состояние виртуального контроллера (Operating State).

## Настройка сети
- `plcsim_set_network`: Настройка связи виртуального ПЛК (Virtual Ethernet Adapter, TCP/IP).
- `plcsim_set_instance_config`: Привязка IP-адресов.
- `plcsim_get_network`: Получить текущие настройки сети (Не реализовано).

## Работа с тегами
- `plcsim_list_tags`: Вывести список всех тегов доступных в симуляции.
- `plcsim_refresh_tags`: Обновить список тегов из симуляции.
- `plcsim_read_tag`: Чтение значения конкретного тега по его имени.
- `plcsim_write_tag`: Запись значения конкретного тега по его имени.
- `plcsim_batch_read`: Одновременное чтение нескольких тегов (Не реализовано).
- `plcsim_batch_write`: Одновременная запись нескольких тегов (Не реализовано).

## Снимки памяти
- `plcsim_save_profile`: Сохранение текущего состояния ПЛК в файл.
- `plcsim_load_profile`: Восстановление состояния ПЛК из ранее созданного файла.
- `plcsim_list_profiles`: Список доступных профилей (Не реализовано).
- `plcsim_delete_profile`: Удаление сохраненного профиля (Не реализовано).
- `plcsim_update_profile`: Обновление существующего профиля (Не реализовано).

## Глобальное управление
- `plcsim_connect`: Подключение к API PLCSIM Advanced.
- `plcsim_disconnect`: Отключение от API (Не реализовано).
- `plcsim_get_simulation_state`: Общее состояние симулятора (Не реализовано).
- `plcsim_status`: Общий статус (Не реализовано).
- `plcsim_get_runtime_config`: (Не реализовано).
- `plcsim_set_runtime_config`: (Не реализовано).
- `plcsim_set_runtime_port`: (Не реализовано).
- `plcsim_start_runtime`: (Не реализовано).
- `plcsim_start_simulation`: (Не реализовано).
- `plcsim_stop_simulation`: (Не реализовано).
- `plcsim_set_widget_value`: (Не реализовано).
