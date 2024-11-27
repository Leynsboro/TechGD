using Newtonsoft.Json;  // Подключаем библиотеки для работы с JSON
using Newtonsoft.Json.Linq;  // Подключаем библиотеки для работы с объектами JSON (JObject)

class Program
{
    // Главный метод программы (точка входа)
    private static void Main()
    {
        // Пути к файлам
        string taskJsonPath = "test/task.json";
        string fileJsonPath = "test/file.json";
        string itemsCsvPath = "test/items.csv";
        // Пути к файлам, которые будем генерировать
        string outputJsonPath = "test/filtered_file.json";
        string outputCsvFilePath = "test/lists_and_contracts.csv";

        // Читаем данные из JSON файлов и преобразуем (JObject.Parse) их в объекты JObject
        JObject taskJson = JObject.Parse(File.ReadAllText(taskJsonPath));
        JObject fileJson = JObject.Parse(File.ReadAllText(fileJsonPath));
        // Читаем все строки из CSV файла
        string[] itemsCsv = File.ReadAllLines(itemsCsvPath);

        // Сейчас в каждом элементе массива itemsCsv хранится строка по типу alien_cosmic,100,20,10
        // А нам нужно иметь возможность обращения к money, details и т.д. у этого item, поэтому
        // Создаем словарь для хранения информации с парой ключ значения (строка, кортэж), где ключ словаря является ключом награды
        var items = new Dictionary<string, (int money, int details, int reputation)>();

        // С помощью цикла foreach проходим по каждому элементу массива itemsCsv (элементы по очереди попадают в переменную line)
        foreach (var line in itemsCsv)
        {
            var columns = line.Split(','); // Разделяем строку на "столбцы" по запятой с помощью метода Split
            // Массив columns теперь хранит в себе 4 элемента, например columns[0] = alien_cosmic (название награды), columns[0] = 100 (награда в рублях) и т.д.

            // Добавляем предмет в словарь используя массив columns
            items[columns[0]] = (   // Задаем название награды ключу элемента словаря items
                money: int.Parse(columns[1]), // Преобразуем в целое число (это нужно сделать обязательно, так как до преобразования это строка)
                                              // значение столбца 2 сохраняем как money
                details: int.Parse(columns[2]), // Преобразуем в целое число значение столбца 3 и сохраняем как details
                reputation: int.Parse(columns[3]) // Преобразуем в целое число значение столбца 4 и сохраняем как reputation
            );
        }


        // Вызываем метод генерации отфильтрованного JSON файла
        GenerateJsonFile(taskJson, fileJson, items, outputJsonPath);

        // Вызываем метод генерации CSV файла
        GenerateCsvFile(taskJson, fileJson, items, outputCsvFilePath);
    }

    // Метод для генерации отфильтрованного JSON файла
    private static void GenerateJsonFile(JObject taskJson, JObject fileJson, Dictionary<string, (int money, int details, int reputation)> items, string outputPath)
    {
        // Создаем список для хранения объектов, которые используются в taskJson
        // Этот список нам пригодится, когда мы будем проверять на наличие object в taskJson
        // Используем множество, а не, например список, чтобы повторные элементы не добавлялись (потому что в множестве элементы уникальные)
        HashSet<string> usedObjects = new HashSet<string>();

        // Проходим по всем элементам в taskJson
        foreach (var list in taskJson.Properties())  // Перебираем все свойства JSON объекта с помощью метода Properties (id, list[])
        {
            var listArray = list.Value["list"];  // Получаем массив "list" для текущего объекта

            foreach (var obj in listArray)  // Проходим по всем объектам в массиве list[]
            {
                // Преобразуем объект в строку (потому что сейчас тип у obj JToken)
                // И добавляем его в множество usedObjects с помощью метода Add
                usedObjects.Add(obj.ToString());
            }
        }

        // Создаем новый JSON объект для сохранения отфильтрованного JSON файла
        JObject filteredJson = new JObject();

        // Проходим по всем элементам в fileJson
        foreach (var fileEntry in fileJson.Properties())  // Перебираем все свойства JSON объекта fileJson (id, reward, weight)
        {
            // Проверяем, если имя текущего объекта (например, object_6813) содержится в множестве используемых объектов
            if (usedObjects.Contains(fileEntry.Name))
            {
                var rewardKey = fileEntry.Value["reward"]?.ToString();  // Получаем значение ключа награды (например, explorer_cosmic)

                var rewardData = items[rewardKey];  // Получаем информацию о награде из словаря items (ключ и кортеж награды)

                // Добавляем объект в итоговый JSON
                filteredJson[fileEntry.Name] = new JObject // Названием объекта указываем текущее его название
                {
                    ["reward"] = rewardKey,  // Добавляем ключ награды
                    ["money"] = rewardData.money,  // Добавляем сумму денег
                    ["details"] = rewardData.details,  // Добавляем количество деталей
                    ["reputation"] = rewardData.reputation  // Добавляем репутацию
                };
            }
        }

        // Записываем итоговый JSON в файл по пути outputPath
        File.WriteAllText(outputPath, JsonConvert.SerializeObject(filteredJson, Formatting.Indented));

        // Выводим сообщение об успешном создании файла для красоты в консоли
        Console.WriteLine($"Файл успешно создан: {outputPath}");
    }

    // Метод для генерации CSV файла
    private static void GenerateCsvFile(JObject taskJson, JObject fileJson, Dictionary<string, (int money, int details, int reputation)> items, string outputPath)
    {
        // Создаем список строк, которые будут записаны в CSV файл
        var csvLines = new List<string>  
        {
            // Добавляем первую строку — заголовки столбцов
            "list_name, object_name, reward_key, money, details, reputation, isUsed"
        };

        // Проходим по всем элементам в taskJson
        foreach (var list in taskJson.Properties())
        {
            var listName = list.Name;  // Получаем имя текущего списка (например, list_1324)
            var listArray = list.Value["list"];  // Получаем массив объектов для текущего списка (list[])

            // Проходим по всем элементам listArray (object_6813, object_4593 и т.д.)
            foreach (var objectName in listArray)  
            {
                string objName = objectName.ToString();  // Преобразуем имя объекта в строку
                string rewardKey = null;  // Переменная для ключа награды (инициализируем как null)
                int money = 0, details = 0, reputation = 0;  // Переменные для хранения данных награды, по умолчанию равны 0
                // Проверяем, есть ли объект в fileJson с помощью тернарного оператора.
                // Если есть, помечаем как упоминающийся (isUsed = 1)
                // Возвращаем с помощью выходного параметра out fileEntry, который хранит в себе свойства объекта (reward и т.д.)
                int isUsed = fileJson.TryGetValue(objName, out var fileEntry) ? 1 : 0;

                // Если объект найден в fileJson (не несуществующий)
                if (fileEntry != null)  
                {
                    rewardKey = fileEntry["reward"].ToString();  // Получаем ключ награды из свойства "reward"

                    var rewardData = items[rewardKey]; // Получаем информацию о награде из словаря items (ключ и кортеж награды)

                    money = rewardData.money;  // Задаем значение money из словаря items
                    details = rewardData.details;  // Задаем значение details из словаря items
                    reputation = rewardData.reputation;  // Задаем значение reputation из словаря items
                }

                // Создаем строку (добавляя элемент в список) для CSV
                csvLines.Add($"{listName},{objName},{rewardKey},{money},{details},{reputation},{isUsed}");
            }
        }

        // Записываем весь CSV файл, соединяя строки списка через символ новой строки
        File.WriteAllText(outputPath, string.Join(Environment.NewLine, csvLines));

        // Выводим сообщение об успешном создании CSV файла для красоты
        Console.WriteLine($"CSV файл успешно создан: {outputPath}");
    }
}
