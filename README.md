#  WebAPI CSV парсер
WebAPI приложение для работы с timescale данными некоторых результатов обработки загружаемоего CSV файла.

Для того что бы импортировать БД, требуется открыть pgAdmin 4. Далее создать базу данных с названием "csv_parse". После того как база данных создана, щелкнуть по ней правой кнопкой мышки и выбрать пункт "Restore" и в строке "Filename" выбрать файл "CSV_Parser_API.sql"? который находиться в архиве "DataBase and test files" приложенный в репозитории к проекту.

Проект запускаеться в Visual Studio через файл "CSV_Parse_API.sln". В архиве так же приложены файлы для тестирования корректной работы.
