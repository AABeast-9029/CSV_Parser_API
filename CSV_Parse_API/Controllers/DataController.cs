using CSV_Parse_API.DataAccess;
using CSV_Parse_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Globalization;

namespace CSV_Parse_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DataController : ControllerBase
    {
        private readonly CsvDbContext _context;

        public DataController(CsvDbContext context)
        {
            _context = context;
        }

        //Обработка и сохранение данных файла в БД [Первый метод]
        [HttpPost("upload")]
        public async Task<IActionResult> UploadCsv(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Файл не предоставлен.");

            if (string.IsNullOrWhiteSpace(file.FileName))
                return BadRequest("Имя файла не может быть пустым.");

            var values = new List<Models.Values>();
            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                string line;
                bool isFirstLine = true; // Флаг для пропуска первой строки
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (isFirstLine)
                    {
                        isFirstLine = false;
                        continue;
                    }

                    var columns = line.Split(';');
                    if (columns.Length != 3)
                        return BadRequest("Неверный формат строки.");

                    if (!DateTime.TryParse(columns[0], out var date) || date < new DateTime(2000, 1, 1) || date > DateTime.UtcNow)
                    {
                        return BadRequest("Дата недопустима. Файл не валидный.");
                    }

                    if (!double.TryParse(columns[1], out var executionTime) || executionTime < 0)
                    {
                        return BadRequest("Время выполнения недопустимо. Файл не валидный.");
                    }


                    if (!double.TryParse(columns[2], out var value) || value < 0)
                    {
                        return BadRequest("Значение в столбце Value недопустимо. Файл не валидный.");
                    }

                    values.Add(new Models.Values { Date = date.ToUniversalTime(), ExecutionTime = executionTime, Value = value, FileName = file.FileName });
                }
            }

            if (values.Count < 1 || values.Count > 10000)
                return BadRequest("Количество строк должно быть от 1 до 10000.");

            _context.Values.AddRange(values);
            await _context.SaveChangesAsync();

            var existingResult = await _context.Results
                .FirstOrDefaultAsync(r => r.FileName == file.FileName);

            // Подсчет и сохранение результатов
            var result = new Models.Results
            {
                FileName = file.FileName,
                FirstOperationTime = values.Min(v => v.Date),
                AverageExecutionTime = values.Average(v => v.ExecutionTime),
                AverageValue = values.Average(v => v.Value),
                MedianValue = CalculateMedian(values.Select(v => v.Value)),
                MaxValue = values.Max(v => v.Value),
                MinValue = values.Min(v => v.Value),
                TimeDelta = (values.Max(v => v.Date) - values.Min(v => v.Date)).TotalSeconds
            };

            if (existingResult != null)
            {
                existingResult.FirstOperationTime = result.FirstOperationTime;
                existingResult.AverageExecutionTime = result.AverageExecutionTime;
                existingResult.AverageValue = result.AverageValue;
                existingResult.MedianValue = result.MedianValue;
                existingResult.MaxValue = result.MaxValue;
                existingResult.MinValue = result.MinValue;
                existingResult.TimeDelta = result.TimeDelta;
            }
            else
            {
                _context.Results.Add(result);
            }

            await _context.SaveChangesAsync();

            var valuesToUpdate = await _context.Values
                .Where(v => v.Results_Id == null && v.FileName == file.FileName)
                .ToListAsync();

            var resultId = await _context.Results
                .Where(r => r.FileName == file.FileName)
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            foreach (var value in valuesToUpdate)
            {
                value.Results_Id = resultId;
            }

            await _context.SaveChangesAsync();
            return Ok("Данные успешно загружены.");
        }

        // Метод для получения результатов по фильтрам [Второй метод]
        [HttpGet("results")]
        public async Task<IActionResult> GetResults([FromQuery] string? fileName, [FromQuery] DateTime? startTime, [FromQuery] DateTime? endTime,
            [FromQuery] double? startAvgValue, [FromQuery] double? endAvgValue, [FromQuery] double? startAvgExecuteTime, [FromQuery] double? endAvgExecuteTime)
        {
            var query = _context.Results.AsQueryable();

            if (!string.IsNullOrEmpty(fileName))
                query = query.Where(r => r.FileName == fileName);

            if (startTime.HasValue && endTime.HasValue && startTime.Value <= endTime.Value)
            {
                DateTime startUtc = startTime.Value.ToUniversalTime();
                DateTime endUtc = endTime.Value.ToUniversalTime();
                query = query.Where(r => r.FirstOperationTime >= startUtc && r.FirstOperationTime <= endUtc);
            }

            if (startAvgValue.HasValue && endAvgValue.HasValue && startAvgValue.Value <= endAvgValue.Value)
                query = query.Where(r => r.AverageValue >= startAvgValue && r.AverageValue <= endAvgValue);

            if (startAvgExecuteTime.HasValue && endAvgExecuteTime.HasValue && startAvgExecuteTime.Value <= endAvgExecuteTime.Value)
                query = query.Where(r => r.AverageExecutionTime >= startAvgExecuteTime && r.AverageExecutionTime <= endAvgExecuteTime);

            var results = await query.ToListAsync();
            if (!results.Any())
            {
                return NotFound("Отсутствуют подходящие записи по данному запросу.");
            }
            return Ok(results);
        }

        // Метод для получения последних 10 значений по имени файла [Третий метод]
        [HttpGet("last-values/{fileName}")]
        public async Task<IActionResult> GetLastValues(string fileName)
        {
            var result = await _context.Results
                .FirstOrDefaultAsync(r => r.FileName == fileName);

            if (result == null)
            {
                return NotFound("Запись не найдена для указанного имени файла.");
            }

            var lastValues = await _context.Values
                .Where(v => v.Results_Id == result.Id)
                .OrderByDescending(v => v.Date)
                .Take(10)
                .ToListAsync();
            if (!lastValues.Any())
            {
                return NotFound("Отсутствуют подходящие записи по данному запросу.");
            }
            return Ok(lastValues);
        }

        private double CalculateMedian(IEnumerable<double> values)
        {
            var sortedValues = values.OrderBy(v => v).ToList();
            int count = sortedValues.Count;
            if (count == 0) return 0;
            if (count % 2 == 0)
                return (sortedValues[count / 2 - 1] + sortedValues[count / 2]) / 2;
            else
                return sortedValues[count / 2];
        }
    }

}
