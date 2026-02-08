using ALARm.Core;
using ALARm.Core.AdditionalParameteres;
using ALARm.Core.Report;
using ALARm.DataAccess;
using AlarmPP.Web.Services;
using BlazorContextMenu;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using System.Text.RegularExpressions;
using OpenCvSharp;
//using OpenCvSharp.Extensions;

using Microsoft.AspNetCore.Components.Web;
namespace AlarmPP.Web.Components.Diagram
{
    public partial class Video : ComponentBase
    {


        // Было:
        // [Parameter]
        // public List<Kilometer> Kilometers { get; set; }

        // Стало:
        private List<Kilometer> _kilometers;

        [Parameter]
        public List<Kilometer> Kilometers
        {
            get => _kilometers;
            set
            {
                _kilometers = value;

                if (_kilometers is { Count: > 0 } && _hasPendingSelection)
                {
                    ApplySelectionFromAppData(_pendingKm, _pendingMeter);
                    _hasPendingSelection = false;

                    // Показать кадр (можно пропустить, если сразу автостарт):
                    //_ = ShowSingleFrameForSelectionAsync();

                    // 👉 Автозапуск после выбора:
                    _ = InvokeAsync(async () =>
                    {
                        await ShowSingleFrameForSelectionAsync(); // опционально
                        await OnTimedEventAsync();                // старт
                    });
                }
            }
        }



        public Kilometer CurrentKm { get; set; }
        public int CurrentVideoFrame = 0;
        public int CurrentMs = 0;
        public int StartMeter { get; set; }
        public int CurrentMeter { get; set; }
        public int[,] Filter { get; set; } = null;
        //[Parameter]

        private DigressionTable DigressionTable { get; set; } = new DigressionTable();
        public string Base64 { get; set; }
        public List<long> FileIdList { get; set; }
        public int Number { get; set; }
        public int N_rows { get; set; }
        public bool ObjectsDialog { get; set; } = false;

        List<Gap> Gaps { get; set; } = new List<Gap>();
        List<Digression> Bolts { get; set; } = new List<Digression>();
        List<Digression> Fasteners { get; set; } = new List<Digression>();
        List<Digression> PerShpals { get; set; } = new List<Digression>();
        List<Digression> DefShpals { get; set; } = new List<Digression>();

        public Image RotateImage(Image img, float rotationAngle)
        {
            Bitmap bmp = new Bitmap(img.Width, img.Height);
            Graphics gfx = Graphics.FromImage(bmp);
            gfx.TranslateTransform((float)bmp.Width / 2, (float)bmp.Height / 2);
            gfx.RotateTransform(rotationAngle);
            gfx.TranslateTransform(-(float)bmp.Width / 2, -(float)bmp.Height / 2);
            gfx.InterpolationMode = InterpolationMode.HighQualityBicubic;
            //gfx.DrawImage(img, new Point(0, 0));
            gfx.DrawImage(img, new System.Drawing.Point(0, 0));

            gfx.Dispose();
            return bmp;
        }

        public void GetFilter(long fileid)
        {
            try
            {
                int carPosition = (int)AppData.Trip.Car_Position;
                Filter = AppData.AdditionalParametersRepository.getFilter(fileid, CurrentMs + 200 * carPosition);
            }
            catch (Exception e)
            {
                Filter = null;
            }
        }

        public int[] OffsetYByFrame = new int[5]; // индивидуальное смещение для каждого из 5 кадров
        public int[] OffsetXByFrame { get; set; } = new int[5]; // по аналогии с OffsetYByFrame


        private long CurrentFileId;

        private async Task MoveFrameUpSafe(int i) => await MoveFrameUp(i);
        private async Task MoveFrameDownSafe(int i) => await MoveFrameDown(i);
        private async Task MoveFrameLeftSafe(int i) => await MoveFrameLeft(i);
        private async Task MoveFrameRightSafe(int i) => await MoveFrameRight(i);
        public float[] RotationAngleByFrame { get; set; } = new float[5];
        public int[] SkewPixelsByFrame { get; set; } = new int[5];

        private async Task RotateFrame(int index, float angleDelta)
        {
            RotationAngleByFrame[index] += angleDelta;
            GetImage2(CurrentFileId);
            await Task.Delay(1);
            StateHasChanged();
        }
        void RotateLeft(int frameIndex)
        {
            RotationAngleByFrame[frameIndex] -= 0.5f;
            GetImage2(CurrentFileId);
        }

        void RotateRight(int frameIndex)
        {
            RotationAngleByFrame[frameIndex] += 0.5f;
            GetImage2(CurrentFileId);
        }
        public async Task MoveFrameUp(int index)
        {
            Console.WriteLine($"MoveFrameUp: index = {index})");

            if (index < 0 || index >= OffsetYByFrame.Length || index == 2)
            {
                Console.WriteLine("Invalid index or forbidden index (2): ignored");
                return;
            }

            OffsetYByFrame[index] -= 5;
            GetImage2(CurrentFileId);
            await Task.Delay(1);
            StateHasChanged();
        }

        public async Task MoveFrameDown(int index)
        {
            Console.WriteLine($"MoveFrameDown called with index = {index})");

            if (index < 0 || index >= OffsetYByFrame.Length || index == 2)
            {
                Console.WriteLine("Blocked by guard clause");
                return;
            }

            OffsetYByFrame[index] += 5;
            Console.WriteLine($"New offset: {OffsetYByFrame[index]}");
            GetImage2(CurrentFileId);
            await Task.Delay(1);
            StateHasChanged();
        }

        public async Task MoveFrameLeft(int index)
        {
            Console.WriteLine($"MoveFrameLeft: index = {index})");

            if (index < 0 || index >= OffsetXByFrame.Length)
            {
                Console.WriteLine("Invalid index: ignored");
                return;
            }

            OffsetXByFrame[index] -= 5;
            GetImage2(CurrentFileId);
            await Task.Delay(1);
            StateHasChanged();
        }

        public async Task MoveFrameRight(int index)
        {
            Console.WriteLine($"MoveFrameRight: index = {index})");

            if (index < 0 || index >= OffsetXByFrame.Length)
            {
                Console.WriteLine("Invalid index: ignored");
                return;
            }

            OffsetXByFrame[index] += 5;
            GetImage2(CurrentFileId);
            await Task.Delay(1);
            StateHasChanged();
        }


        public float CenterScaleY { get; set; } = 1.0f; // по умолчанию 70% высоты

        public void SetCenterScaleY(ChangeEventArgs e)
        {
            if (float.TryParse(e.Value?.ToString(), out float result))
            {
                CenterScaleY = Math.Clamp(result / 100f, 0.1f, 1.0f);
                GetImage2(CurrentFileId);
            }
        }
        public float[] OffsetKoefByFrame { get; set; } = new float[5];
        private readonly string OffsetSavePath = Path.Combine("C:\\sntfi\\Alarm5\\alarmvideo_offset", "offsets.json");

        private class OffsetSaveModel
        {
            public int[] OffsetY { get; set; }
            public int[] OffsetX { get; set; } // добавлено
            public float[] KoefY { get; set; }

            public float[] Brightness { get; set; }     // ← добавляем яркость
            public float CenterScaleY { get; set; }     // ← добавляем высоту центрального кадра

        }

        public int LastFrameHeight { get; set; } = 0;
        private void EnsureFrameBrightnessInitialized()
        {
            if (AppData.FrameBrightness == null || AppData.FrameBrightness.Length <= 5)
            {
                AppData.FrameBrightness = new float[] { 1f, 1f, 1f, 1f, 1f };
            }
        }

        public void SaveOffsetsInMemory()
        {
            int H = LastFrameHeight > 0 ? LastFrameHeight : 1;
            for (int i = 0; i < 5; i++)
            {
                OffsetKoefByFrame[i] = (float)OffsetYByFrame[i] / H;
            }

            SaveOffsetsToFile(); // 💾 сохраняем в JSON
        }
        private readonly string BrightnessFilePath = @"C:\sntfi\Alarm5\alarmvideo_offset\brightness.json";

        private void SaveBrightness()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(BrightnessFilePath));
                File.WriteAllText(BrightnessFilePath, JsonSerializer.Serialize(AppData.FrameBrightness));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка сохранения яркости: " + ex.Message);
            }
        }
        private void LoadBrightness()
        {
            try
            {
                if (File.Exists(BrightnessFilePath))
                {
                    var json = File.ReadAllText(BrightnessFilePath);
                    var loaded = JsonSerializer.Deserialize<float[]>(json);

                    if (loaded != null && loaded.Length == 5)
                        AppData.FrameBrightness = loaded;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка загрузки яркости: " + ex.Message);
            }
        }



        protected override void OnInitialized()
        {
            // ЛОГИ ПРОВЕРКИ СЕРВИСОВ
            Console.WriteLine($"[DEBUG] AppData is null: {AppData == null}");
            Console.WriteLine($"[DEBUG] AppData.Trip is null: {AppData?.Trip.Id == null}");
            Console.WriteLine($"[DEBUG] AppData.VideoProcessing: {AppData?.VideoProcessing}");
            Console.WriteLine($"[DEBUG] AppData.RdStructureRepository is null: {AppData?.RdStructureRepository == null}");

            // ОСТАВЛЯЕМ СУЩЕСТВУЮЩИЙ КОД
            LoadBrightness();
            EnsureFrameBrightnessInitialized(); // ← только 1 раз
            LoadOffsetsFromFile(); // 🡐 загружаем если есть
            SyncHudFromState();
        }
        public void SaveOffsetsToFile()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(OffsetSavePath));

                var saveData = new OffsetSaveModel
                {
                    OffsetY = OffsetYByFrame, // ← конвертируем int[] → float[]
                    OffsetX = OffsetXByFrame, // сохраняем X

                    KoefY = OffsetKoefByFrame,
                    Brightness = AppData.FrameBrightness,
                    CenterScaleY = CenterScaleY
                };

                string json = JsonSerializer.Serialize(saveData, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(OffsetSavePath, json);

                Console.WriteLine("✅ Смещения, яркость и масштаб центра сохранены: " + OffsetSavePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Ошибка при сохранении: " + ex.Message);
            }
        }
        public float GlobalBrightnessFactor { get; set; } = 1.0f;

        private float[] AdjustBrightnessToMatch(List<Bitmap> frames)
        {
            float[] luminances = new float[frames.Count];

            for (int i = 0; i < frames.Count; i++)
                luminances[i] = GetAverageBrightness(frames[i]);

            float avg = luminances.Average();

            float[] correctionFactors = new float[frames.Count];
            for (int i = 0; i < frames.Count; i++)
                correctionFactors[i] = luminances[i] > 0.01f ? avg / luminances[i] : 1f;

            return correctionFactors;
        }
        public void OnGlobalBrightnessChanged(ChangeEventArgs e)
        {
            if (float.TryParse(e.Value?.ToString(), out float newValue))
            {
                GlobalBrightnessFactor = newValue;
                NormalizeBrightnessManual(); // обновим сразу
            }
        }

        public void NormalizeBrightnessManual()
        {
            try
            {
                var frames = (List<Bitmap>)AppData.AdditionalParametersRepository.getBitMaps(
                    CurrentFileId,
                    CurrentMs,
                    CurrentVideoFrame,
                    RepType.Undefined
                )["bitMaps"];

                float[] luminances = frames.Select(GetAverageBrightness).ToArray();
                float avg = luminances.Average();

                for (int i = 0; i < 5; i++)
                {
                    float norm = avg / (luminances[i] > 0.01f ? luminances[i] : 1f);
                    AppData.FrameBrightness[i] = norm * GlobalBrightnessFactor;
                }

                Console.WriteLine("✅ Усреднение с множителем: " + GlobalBrightnessFactor.ToString("0.00"));
                GetImage2(CurrentFileId);
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Ошибка нормализации яркости: " + ex.Message);
            }
        }


        private float GetAverageBrightness(Bitmap bmp)
        {
            float sum = 0;
            int width = bmp.Width;
            int height = bmp.Height;

            BitmapData data = bmp.LockBits(new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

            unsafe
            {
                byte* ptr = (byte*)data.Scan0;
                int stride = data.Stride;

                for (int y = 0; y < height; y++)
                {
                    byte* row = ptr + (y * stride);
                    for (int x = 0; x < width; x++)
                    {
                        byte b = row[x * 3];
                        byte g = row[x * 3 + 1];
                        byte r = row[x * 3 + 2];

                        float lum = 0.299f * r + 0.587f * g + 0.114f * b;
                        sum += lum;
                    }
                }
            }

            bmp.UnlockBits(data);
            return sum / (width * height * 255.0f); // нормализуем
        }


        public void LoadOffsetsFromFile()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(OffsetSavePath)); // гарантируем, что папка существует

                if (!File.Exists(OffsetSavePath))
                {
                    Console.WriteLine("📁 Файл смещений не найден, создаю новый...");

                    // создаём файл с текущими значениями
                    var defaultData = new OffsetSaveModel
                    {
                        OffsetY = OffsetYByFrame ?? new int[5],
                        OffsetX = OffsetXByFrame ?? new int[5],
                        KoefY = OffsetKoefByFrame ?? new float[5],
                        CenterScaleY = CenterScaleY
                    };

                    string newJson = JsonSerializer.Serialize(defaultData, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(OffsetSavePath, newJson);

                    Console.WriteLine("✅ Новый файл смещений создан: " + OffsetSavePath);
                    return;
                }

                // файл существует — читаем и загружаем
                string json = File.ReadAllText(OffsetSavePath);
                var loaded = JsonSerializer.Deserialize<OffsetSaveModel>(json);

                if (loaded != null)
                {
                    if (loaded.OffsetY?.Length == 5)
                    {
                        OffsetYByFrame = loaded.OffsetY;
                        Console.WriteLine("✅ Смещения OffsetY загружены.");
                    }
                    if (loaded.OffsetX?.Length == 5)
                    {
                        OffsetXByFrame = loaded.OffsetX;
                        Console.WriteLine("✅ Смещения OffsetX загружены.");
                    }
                    if (loaded.KoefY?.Length == 5)
                    {
                        OffsetKoefByFrame = loaded.KoefY;
                        Console.WriteLine("✅ Коэффициенты KoefY загружены.");
                    }


                    if (loaded.CenterScaleY > 0.05f && loaded.CenterScaleY <= 2.0f)
                    {
                        CenterScaleY = loaded.CenterScaleY;
                        Console.WriteLine($"✅ Центр. масштаб загружен: {CenterScaleY:0.00}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Ошибка при загрузке/создании файла смещений: " + ex.Message);
            }
        }



        private readonly struct CamFrame
        {
            public readonly Bitmap Bmp;
            public readonly long TsNs;
            public CamFrame(Bitmap bmp, long tsNs) { Bmp = bmp; TsNs = tsNs; }
        }

        private readonly struct SyncParams
        {
            public readonly double A; public readonly double B;
            public SyncParams(double a, double b) { A = a; B = b; }
            public long Map(long tNs) => (long)Math.Round(A * tNs + B);
        }



        // Вверху класса Video (поле):
        private const bool VerboseLogs = false;
        private static void Log(string msg) { if (VerboseLogs) Console.WriteLine(msg); }



        // Ручной вертикальный сдвиг для камер 0..4 (px).
        // Отрицательное — вверх, положительное — вниз.
        public int[] ManualYOffsetByCam { get; set; } = new int[5];


        // Удобные помощники (по желанию)
        private void Redraw() { GetImage2(CurrentFileId); StateHasChanged(); }

        // сдвиг содержимого внутри плитки для каждой из 5 камер (px)
        // >0 — вверх (пэддинг внизу), <0 — вниз (пэддинг сверху)
        public int[] InnerShiftYByCam { get; set; } = new int[5];
        // ===== ЗАМЕНИ СВОЙ GetImage2 НА ЭТУ ВЕРСИЮ =====
        // ===== замени свой GetImage2 на этот =====
        // Параметры "антишва"


        public int PlaybackSpeed { get; set; } = 1; // 1 = обычная скорость
        /// <summary>

        // === РЕВЕРС-ПЛЕЙ ===
        private CancellationTokenSource _rewindCts;
        private Task _rewindTask;
        // Уже есть IsPlaying => AppData.VideoProcessing
        private bool IsRewinding => _rewindCts != null && !_rewindCts.IsCancellationRequested;

        // Кадров за один «тик» реверса (можно увеличить для более быстрой перемотки)
        public int ReverseFramesPerTick { get; set; } = 1;

        // Задержка между тиками реверса (мс). Если хочешь, используй AppData.Speed.
        public int ReverseDelayMs { get; set; } = 50;

        // Запуск реверса
        public async Task StartReverseAsync()
        {
            // стоп обычного воспроизведения (важно, иначе оба цикла будут жить)
            AppData.VideoProcessing = false;
            try { _playCts?.Cancel(); } catch { /* ignore */ }
            _playCts?.Dispose();
            _playCts = null;

            // если уже идёт реверс — не дублируем
            if (IsRewinding) return;

            _rewindCts?.Dispose();
            _rewindCts = new CancellationTokenSource();
            _rewindTask = RunReverseLoopAsync(_rewindCts.Token);

            await InvokeAsync(StateHasChanged);
        }

        public async Task StopReverse()
        {
            _rewindCts?.Cancel();
            await InvokeAsync(StateHasChanged); // чтобы UI обновился
        }

        private async Task RunReverseLoopAsync(CancellationToken token)
        {
            try
            {
                if (Kilometers == null || Kilometers.Count == 0 || CurrentKm == null)
                    return;

                // При старте реверса перепроверим наличие файла
                var fileIds = AppData.RdStructureRepository.GetFileID(AppData.Trip.Id, CurrentKm.Number);
                if (fileIds == null || fileIds.Count == 0)
                    return;

                while (!token.IsCancellationRequested)
                {
                    // если вдруг включили обычное воспроизведение — выходим
                    if (AppData.VideoProcessing) break;

                    CurrentVideoFrame -= ReverseFramesPerTick;
                    CurrentMs -= 200 * ReverseFramesPerTick;
                    CurrentMeter = StartMeter + (CurrentVideoFrame / 5);

                    if (CurrentVideoFrame < 0 || CurrentMs < 0)
                    {
                        CurrentVideoFrame = 0;
                        CurrentMs = 0;
                        CurrentMeter = StartMeter;
                        GetImage2(fileIds[0]);
                        try { await InvokeAsync(StateHasChanged); } catch { }
                        break;
                    }

                    GetImage2(fileIds[0]);
                    try { await InvokeAsync(StateHasChanged); } catch { /* ignore */ }

                    try { await Task.Delay(ReverseDelayMs, token); }
                    catch (TaskCanceledException) { break; }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] RunReverseLoopAsync: {ex.Message}");
            }
            finally
            {
                try { await InvokeAsync(StateHasChanged); } catch { /* ignore */ }
            }
        }


        private bool AssembleLeftToRight()
        {
            int dir = Math.Sign((int)(CurrentKm?.Direction ?? 0));
            int car = Math.Sign((int)(AppData?.Trip?.Car_Position ?? 0));
            if (dir == 0 || car == 0) return true;
            return dir == car;
        }
        public List<int> MeterLines { get; private set; } = new();
        // Включатель калибровки
        private bool _enableRailRectify = true;

        // Сам калибратор (создай как тебе удобно)
        private readonly AlarmPP.Web.Services.VideoRailRectifier _railRectifier =
            new AlarmPP.Web.Services.VideoRailRectifier(new AlarmPP.Web.Services.VideoRailRectifier.Settings());
        public bool BadCam3Mode { get; set; } = true;

        // сколько пикселей “выпирает” вверх (отрисовкой)
        const int Cam3_DrawUpPx = 30;
        private void SaveFramesToTemp(List<Bitmap> frames, int rowIndex)
        {
            try
            {
                var dir = @"C:\sntfi\temp";
                Directory.CreateDirectory(dir);

                for (int i = 0; i < frames.Count; i++)
                {
                    var path = Path.Combine(
                        dir,
                        $"row_{rowIndex:00}_cam{i}.png"
                    );

                    frames[i].Save(path, ImageFormat.Png);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[TEMP] " + ex.Message);
            }
        }
        private int _debugSavedRows = 0;


        public void GetImage2(long fileid)
        {
            try
            {
                CurrentFileId = fileid;

                int carPosition = (int)AppData.Trip.Car_Position;
                int direction = (int)CurrentKm.Direction;
                if (direction == 0) direction = 1;

                const int RowOverlap = 35; // зона горизонтальной склейки между рядами (по Y)
                const int SearchDy = 3;

                // ===== ТЮНИНГ =====
                const float Cam3_RotateDeg = -1.2f;   // поворот cam3 (когда BadCam3Mode==true)
                const int Cam3_DrawUpPx = 20;         // поднимаем cam3 "выпиранием" вверх (только при BadCam3Mode==true)
                const int Cam34_ShiftRightPx = 0;     // оставлено для совместимости (не используем)

                // Осветление (ручное): "2-я слева" и "центр"
                const float GainSecondFromLeft = 1.12f;
                const float GainCenter = 1.18f;

                // ===== АВТО-ВЫРАВНИВАНИЕ ОСВЕЩЕНИЯ (ОДИН ПАРАМЕТР ДЛЯ ВСЕХ) =====
                const bool AutoNormalizeExposure = true;

                // 0..255: чем больше — тем светлее будут ВСЕ камеры
                const float TargetLuma = 92f;

                // ограничения, чтобы не было сильного пересвета/затемнения
                const float MinAutoGain = 0.85f;
                const float MaxAutoGain = 1.35f;

                // доп. общий множитель (если захочешь одним числом сделать светлее/темнее вообще всё)
                const float GlobalExposure = 1.00f;

                // Лечение чёрных пустот (в финале)
                const byte BlackThr = 8;
                const int MaxRadius = 18;

                // ===== ФЛАГИ "ПЛОХОЙ КАДР" ПО КАМЕРАМ =====
                bool[] BadCam = new bool[5];
                BadCam[3] = BadCam3Mode;

                // ===== 1) первый ряд =====
                var firstRes = AppData.AdditionalParametersRepository
                    .getBitMaps(fileid, CurrentMs, CurrentVideoFrame, RepType.Undefined);

                if (firstRes?["bitMaps"] is not List<Bitmap> firstFive || firstFive.Count < 5)
                {
                    Base64 = null;
                    return;
                }
                if (_debugSavedRows < 10)
                {
                    SaveFramesToTemp(firstFive, _debugSavedRows);
                    _debugSavedRows++;
                }

                int W = firstFive[0].Width;
                int H = firstFive[0].Height;
                LastFrameHeight = H;

                // ===== 2) загружаем строки =====
                var rows = new List<List<Bitmap>> { firstFive };

                int stepMs = 50 * PlaybackSpeed * Math.Max(1, Math.Abs(carPosition)) * direction;
                int baseMs = CurrentMs;
                int baseFno = CurrentVideoFrame;

                for (int i = 1; i < N_rows; i++)
                {
                    baseMs += stepMs;
                    baseFno += direction * Math.Max(1, Math.Abs(carPosition)) * PlaybackSpeed;

                    var r = AppData.AdditionalParametersRepository
                        .getBitMaps(fileid, baseMs, baseFno, RepType.Undefined);

                    if (r?["bitMaps"] is not List<Bitmap> five || five.Count < 5)
                        break;

                    rows.Add(five);
                }

                int rowCount = rows.Count;

                // ===== 3) холст =====
                int baseRowH = H;
                int totalH = baseRowH + (rowCount - 1) * (baseRowH - RowOverlap);

                using var canvas = new Bitmap(W * 5, totalH, PixelFormat.Format24bppRgb);
                using var gCanvas = Graphics.FromImage(canvas);

                gCanvas.Clear(Color.Black);
                gCanvas.InterpolationMode = InterpolationMode.NearestNeighbor;
                gCanvas.SmoothingMode = SmoothingMode.None;
                gCanvas.CompositingMode = CompositingMode.SourceCopy;
                gCanvas.PixelOffsetMode = PixelOffsetMode.HighQuality;

                // ===== helpers =====
                static Bitmap MirrorX(Bitmap src)
                {
                    var c = (Bitmap)src.Clone();
                    c.RotateFlip(RotateFlipType.RotateNoneFlipX);
                    return c;
                }

                // Поворот без “лечения”, но с wrap, чтобы не давал чёрных швов по краю
                static Bitmap RotateSmall(Bitmap src, float angleDeg)
                {
                    if (Math.Abs(angleDeg) < 0.001f)
                        return (Bitmap)src.Clone();

                    int w = src.Width;
                    int h = src.Height;

                    var dst = new Bitmap(w, h, PixelFormat.Format24bppRgb);

                    using (var g = Graphics.FromImage(dst))
                    using (var ia = new ImageAttributes())
                    {
                        ia.SetWrapMode(WrapMode.TileFlipXY);

                        g.Clear(Color.Black);
                        g.CompositingMode = CompositingMode.SourceCopy;
                        g.InterpolationMode = InterpolationMode.NearestNeighbor;
                        g.SmoothingMode = SmoothingMode.None;
                        g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                        g.TranslateTransform(w / 2f, h / 2f);
                        g.RotateTransform(angleDeg);
                        g.TranslateTransform(-w / 2f, -h / 2f);

                        g.DrawImage(src, new Rectangle(0, 0, w, h), 0, 0, w, h, GraphicsUnit.Pixel, ia);
                    }

                    return dst;
                }

                static void DrawWithGain(Graphics g, Bitmap bmp, int destX, int W, int H, float gain)
                {
                    if (Math.Abs(gain - 1f) < 0.0001f)
                    {
                        g.DrawImage(bmp, new Rectangle(destX, 0, W, H), new Rectangle(0, 0, W, H), GraphicsUnit.Pixel);
                        return;
                    }

                    using var ia = new ImageAttributes();
                    var cm = new ColorMatrix(new float[][]
                    {
                new float[] { gain, 0,    0,    0, 0 },
                new float[] { 0,    gain, 0,    0, 0 },
                new float[] { 0,    0,    gain, 0, 0 },
                new float[] { 0,    0,    0,    1, 0 },
                new float[] { 0,    0,    0,    0, 1 }
                    });

                    ia.SetColorMatrix(cm, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

                    g.DrawImage(bmp,
                        new Rectangle(destX, 0, W, H),
                        0, 0, W, H,
                        GraphicsUnit.Pixel,
                        ia);
                }

                // Рисование на CANVAS со сдвигом по Y (выпирание)
                static void DrawWithGainY_OnCanvas(Graphics g, Bitmap bmp, int destX, int destY, int W, int H, float gain)
                {
                    using var ia = new ImageAttributes();

                    if (Math.Abs(gain - 1f) >= 0.0001f)
                    {
                        var cm = new ColorMatrix(new float[][]
                        {
                    new float[] { gain, 0,    0,    0, 0 },
                    new float[] { 0,    gain, 0,    0, 0 },
                    new float[] { 0,    0,    gain, 0, 0 },
                    new float[] { 0,    0,    0,    1, 0 },
                    new float[] { 0,    0,    0,    0, 1 }
                        });

                        ia.SetColorMatrix(cm, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                    }

                    g.DrawImage(bmp,
                        new Rectangle(destX, destY, W, H),
                        0, 0, W, H,
                        GraphicsUnit.Pixel,
                        ia);
                }

                static unsafe void BlendTopOverlapAll(Bitmap canvasBmp, Bitmap rowStrip, int destY, int overlapH)
                {
                    if (overlapH <= 0) return;

                    int Wc = canvasBmp.Width;
                    int Hc = canvasBmp.Height;

                    if (destY < 0) { overlapH += destY; destY = 0; }
                    if (destY >= Hc) return;

                    overlapH = Math.Min(overlapH, rowStrip.Height);
                    overlapH = Math.Min(overlapH, Hc - destY);
                    if (overlapH <= 0) return;

                    var rc = new Rectangle(0, destY, Wc, overlapH);
                    var rr = new Rectangle(0, 0, Wc, overlapH);

                    var dc = canvasBmp.LockBits(rc, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
                    var dr = rowStrip.LockBits(rr, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

                    try
                    {
                        byte* pc0 = (byte*)dc.Scan0;
                        byte* pr0 = (byte*)dr.Scan0;
                        int sc = dc.Stride;
                        int sr = dr.Stride;

                        int denom = Math.Max(1, overlapH - 1);

                        for (int y = 0; y < overlapH; y++)
                        {
                            float a = y / (float)denom;
                            float ia = 1f - a;

                            byte* pc = pc0 + y * sc;
                            byte* pr = pr0 + y * sr;

                            for (int x = 0; x < Wc; x++)
                            {
                                int i3 = x * 3;
                                pc[i3 + 0] = (byte)(pc[i3 + 0] * ia + pr[i3 + 0] * a);
                                pc[i3 + 1] = (byte)(pc[i3 + 1] * ia + pr[i3 + 1] * a);
                                pc[i3 + 2] = (byte)(pc[i3 + 2] * ia + pr[i3 + 2] * a);
                            }
                        }
                    }
                    finally
                    {
                        rowStrip.UnlockBits(dr);
                        canvasBmp.UnlockBits(dc);
                    }
                }

                static unsafe int FindBestDyForRowSeam(Bitmap canvasBmp, int yCursor, Bitmap rowStrip, int overlapH, int searchDy)
                {
                    int bestDy = 0;
                    long bestErr = long.MaxValue;

                    int Wc = canvasBmp.Width;
                    int Hc = canvasBmp.Height;

                    for (int dy = -searchDy; dy <= searchDy; dy++)
                    {
                        int yTop = yCursor - overlapH - dy;
                        if (yTop < 0) continue;
                        if (yTop + overlapH > Hc) continue;

                        var rc = new Rectangle(0, yTop, Wc, overlapH);
                        var rr = new Rectangle(0, 0, Wc, overlapH);

                        var dc = canvasBmp.LockBits(rc, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                        var dr = rowStrip.LockBits(rr, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

                        try
                        {
                            byte* pc0 = (byte*)dc.Scan0;
                            byte* pr0 = (byte*)dr.Scan0;
                            int sc = dc.Stride;
                            int sr = dr.Stride;

                            long err = 0;

                            for (int y = 0; y < overlapH; y++)
                            {
                                byte* pc = pc0 + y * sc;
                                byte* pr = pr0 + y * sr;

                                for (int x = 0; x < Wc; x++)
                                {
                                    int i3 = x * 3;
                                    int db = pc[i3 + 0] - pr[i3 + 0];
                                    int dg = pc[i3 + 1] - pr[i3 + 1];
                                    int drc = pc[i3 + 2] - pr[i3 + 2];
                                    err += (long)(db * db + dg * dg + drc * drc);
                                }
                            }

                            if (err < bestErr)
                            {
                                bestErr = err;
                                bestDy = dy;
                            }
                        }
                        finally
                        {
                            rowStrip.UnlockBits(dr);
                            canvasBmp.UnlockBits(dc);
                        }
                    }

                    return bestDy;
                }

                static unsafe void FillBlackHoles(Bitmap bmp, byte thr, int maxRadius)
                {
                    var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
                    var data = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

                    try
                    {
                        int w = bmp.Width;
                        int h = bmp.Height;
                        int stride = data.Stride;
                        byte* p0 = (byte*)data.Scan0;

                        bool IsBlack(byte* px) => px[0] <= thr && px[1] <= thr && px[2] <= thr;

                        for (int y = 0; y < h; y++)
                        {
                            byte* row = p0 + y * stride;
                            for (int x = 0; x < w; x++)
                            {
                                byte* px = row + x * 3;
                                if (!IsBlack(px)) continue;

                                bool found = false;
                                byte nb = 0, ng = 0, nr = 0;

                                for (int r = 1; r <= maxRadius && !found; r++)
                                {
                                    int y0 = Math.Max(0, y - r);
                                    int y1 = Math.Min(h - 1, y + r);
                                    int x0 = Math.Max(0, x - r);
                                    int x1 = Math.Min(w - 1, x + r);

                                    for (int xx = x0; xx <= x1 && !found; xx++)
                                    {
                                        byte* pTop = p0 + y0 * stride + xx * 3;
                                        if (!IsBlack(pTop)) { nb = pTop[0]; ng = pTop[1]; nr = pTop[2]; found = true; break; }

                                        byte* pBot = p0 + y1 * stride + xx * 3;
                                        if (!IsBlack(pBot)) { nb = pBot[0]; ng = pBot[1]; nr = pBot[2]; found = true; break; }
                                    }

                                    for (int yy = y0; yy <= y1 && !found; yy++)
                                    {
                                        byte* pL = p0 + yy * stride + x0 * 3;
                                        if (!IsBlack(pL)) { nb = pL[0]; ng = pL[1]; nr = pL[2]; found = true; break; }

                                        byte* pR = p0 + yy * stride + x1 * 3;
                                        if (!IsBlack(pR)) { nb = pR[0]; ng = pR[1]; nr = pR[2]; found = true; break; }
                                    }
                                }

                                if (found)
                                {
                                    px[0] = nb; px[1] = ng; px[2] = nr;
                                }
                            }
                        }
                    }
                    finally
                    {
                        bmp.UnlockBits(data);
                    }
                }

                static unsafe float ComputeMeanLumaFast(Bitmap bmp)
                {
                    if (bmp == null) return 0f;

                    var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
                    var data = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

                    try
                    {
                        int w = bmp.Width;
                        int h = bmp.Height;
                        int stride = data.Stride;
                        byte* p0 = (byte*)data.Scan0;

                        // подвыборка, чтобы не тормозило
                        int sx = Math.Max(1, w / 140);
                        int sy = Math.Max(1, h / 90);

                        long sum = 0;
                        long cnt = 0;

                        for (int y = 0; y < h; y += sy)
                        {
                            byte* row = p0 + y * stride;
                            for (int x = 0; x < w; x += sx)
                            {
                                byte* px = row + x * 3; // BGR
                                int b = px[0];
                                int g = px[1];
                                int r = px[2];

                                int luma = (299 * r + 587 * g + 114 * b + 500) / 1000;
                                sum += luma;
                                cnt++;
                            }
                        }

                        if (cnt == 0) return 0f;
                        return (float)sum / cnt;
                    }
                    finally
                    {
                        bmp.UnlockBits(data);
                    }
                }

                // ===== 4) сборка по рядам =====
                int yCursor = 0;

                for (int i = 0; i < rowCount; i++)
                {
                    using var rowStrip = new Bitmap(W * 5, baseRowH, PixelFormat.Format24bppRgb);
                    using var gRow = Graphics.FromImage(rowStrip);

                    gRow.Clear(Color.Black);
                    gRow.InterpolationMode = InterpolationMode.NearestNeighbor;
                    gRow.SmoothingMode = SmoothingMode.None;
                    gRow.CompositingMode = CompositingMode.SourceCopy;
                    gRow.PixelOffsetMode = PixelOffsetMode.HighQuality;

                    // Для выпирания cam3: сохраняем кадр cam3 (уже mirrored) и gain
                    Bitmap cam3ForOverlay = null;
                    float cam3GainForOverlay = 1f;

                    Bitmap[] prepared = new Bitmap[5];

                    try
                    {
                        // mirror
                        for (int j = 0; j < 5; j++)
                            prepared[j] = MirrorX(rows[i][j]);

                        for (int j = 0; j < 5; j++)
                        {
                            int visualSlot = 4 - j; // слева->направо
                            int destX = visualSlot * W;

                            // базовый ручной gain (как было)
                            float gain = 1f;
                            if (visualSlot == 1) gain = GainSecondFromLeft;
                            if (visualSlot == 2) gain = GainCenter;

                            // авто-нормализация, чтобы все камеры стали одинаковыми по свету
                            if (AutoNormalizeExposure)
                            {
                                float mean = ComputeMeanLumaFast(prepared[j]);
                                if (mean > 1f)
                                {
                                    float autoGain = TargetLuma / mean;
                                    autoGain = Math.Clamp(autoGain, MinAutoGain, MaxAutoGain);
                                    gain *= autoGain;
                                }
                            }

                            // общий множитель "сделать ярче/темнее всё"
                            gain *= GlobalExposure;

                            // ===== cam3 (j==3) =====
                            if (j == 3)
                            {
                                // в rowStrip cam3 рисуем НОРМАЛЬНО (чтобы швы по горизонтали склеились!)
                                DrawWithGain(gRow, prepared[3], destX, W, baseRowH, gain);

                                // а если BadCam3Mode==true — потом поверх на canvas сделаем поднятие + поворот
                                if (BadCam[3])
                                {
                                    cam3ForOverlay = (Bitmap)prepared[3].Clone();
                                    cam3GainForOverlay = gain;
                                }

                                continue;
                            }

                            // остальные камеры
                            DrawWithGain(gRow, prepared[j], destX, W, baseRowH, gain);
                        }
                    }
                    finally
                    {
                        for (int j = 0; j < 5; j++)
                            prepared[j]?.Dispose();
                    }

                    // --- dy выравнивание между рядами ---
                    int bestDy = 0;
                    if (i > 0)
                    {
                        bestDy = FindBestDyForRowSeam(canvas, yCursor, rowStrip, RowOverlap, SearchDy);
                        bestDy = Math.Clamp(bestDy, -SearchDy, SearchDy);
                    }

                    int yDraw = yCursor - bestDy;

                    // 1) кладём ряд на canvas (со склейкой по горизонтали)
                    if (i == 0)
                    {
                        gCanvas.DrawImage(rowStrip,
                            new Rectangle(0, yDraw, rowStrip.Width, rowStrip.Height),
                            new Rectangle(0, 0, rowStrip.Width, rowStrip.Height),
                            GraphicsUnit.Pixel);
                    }
                    else
                    {
                        BlendTopOverlapAll(canvas, rowStrip, yDraw, RowOverlap);

                        int ySrc = RowOverlap;
                        int hCopy = rowStrip.Height - RowOverlap;
                        if (hCopy > 0)
                        {
                            gCanvas.DrawImage(rowStrip,
                                new Rectangle(0, yDraw + RowOverlap, rowStrip.Width, hCopy),
                                new Rectangle(0, ySrc, rowStrip.Width, hCopy),
                                GraphicsUnit.Pixel);
                        }
                    }

                    // 2) Теперь делаем cam3 "выпирание" (поднятие) БЕЗ потери склейки:
                    //    - base cam3 уже в rowStrip и уже склеилась
                    //    - overlay просто "поднимает" и чуть поворачивает поверх
                    if (BadCam[3] && cam3ForOverlay != null)
                    {
                        // x cam3: j==3 => visualSlot==1 => x = 1*W
                        int cam3X = 1 * W;
                        int cam3Y = yDraw - Cam3_DrawUpPx;

                        Bitmap src = cam3ForOverlay;

                        if (Math.Abs(Cam3_RotateDeg) > 0.001f)
                        {
                            var rot = RotateSmall(src, Cam3_RotateDeg);
                            src.Dispose();
                            src = rot;
                        }

                        var oldComp = gCanvas.CompositingMode;
                        gCanvas.CompositingMode = CompositingMode.SourceCopy;

                        DrawWithGainY_OnCanvas(gCanvas, src, cam3X, cam3Y, W, baseRowH, cam3GainForOverlay);

                        gCanvas.CompositingMode = oldComp;

                        src.Dispose();
                        cam3ForOverlay = null;
                    }

                    // шаг по Y
                    int step = (baseRowH - RowOverlap);
                    yCursor += (step - bestDy);

                    cam3ForOverlay?.Dispose();
                }

                // ===== 4.5) лечим чёрные пустоты (после всех наложений) =====
                FillBlackHoles(canvas, BlackThr, MaxRadius);

                // ===== 5) метровые линии =====
                using (var gFinal = Graphics.FromImage(canvas))
                using (var pen = new Pen(Color.FromArgb(215, 15, 30), 2.5f))
                {
                    pen.Alignment = PenAlignment.Center;

                    int y = 0;
                    for (int i = 0; i < rowCount; i++)
                    {
                        if ((CurrentVideoFrame + i) % 5 == 0)
                            gFinal.DrawLine(pen, 0, y, canvas.Width, y);

                        y += (baseRowH - RowOverlap);
                    }
                }

                // ===== 6) PNG =====
                using var ms = new MemoryStream();
                canvas.Save(ms, ImageFormat.Png);
                Base64 = Convert.ToBase64String(ms.ToArray());
            }
            catch
            {
                Base64 = null;
            }
        }











        private static unsafe void FillBlackHoles(Bitmap bmp, byte blackThr, int maxRadius)
        {
            if (bmp == null) return;
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb) return;
            if (maxRadius <= 0) return;

            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            var data = bmp.LockBits(rect, ImageLockMode.ReadWrite, bmp.PixelFormat);

            try
            {
                int w = bmp.Width;
                int h = bmp.Height;
                int stride = data.Stride;
                byte* basePtr = (byte*)data.Scan0;

                bool IsBlack(byte* p) => p[0] <= blackThr && p[1] <= blackThr && p[2] <= blackThr;

                for (int y = 1; y < h - 1; y++)
                {
                    byte* row = basePtr + y * stride;
                    for (int x = 1; x < w - 1; x++)
                    {
                        byte* p = row + x * 3;
                        if (!IsBlack(p)) continue;

                        bool found = false;
                        byte bestB = 0, bestG = 0, bestR = 0;

                        for (int r = 1; r <= maxRadius && !found; r++)
                        {
                            int x0 = Math.Max(0, x - r);
                            int x1 = Math.Min(w - 1, x + r);
                            int y0 = Math.Max(0, y - r);
                            int y1 = Math.Min(h - 1, y + r);

                            // верх/низ
                            byte* rowTop = basePtr + y0 * stride;
                            byte* rowBot = basePtr + y1 * stride;

                            for (int xx = x0; xx <= x1; xx++)
                            {
                                byte* pt = rowTop + xx * 3;
                                if (!IsBlack(pt)) { bestB = pt[0]; bestG = pt[1]; bestR = pt[2]; found = true; break; }

                                byte* pb = rowBot + xx * 3;
                                if (!IsBlack(pb)) { bestB = pb[0]; bestG = pb[1]; bestR = pb[2]; found = true; break; }
                            }

                            // лево/право
                            if (!found)
                            {
                                for (int yy = y0; yy <= y1; yy++)
                                {
                                    byte* rr = basePtr + yy * stride;

                                    byte* pl = rr + x0 * 3;
                                    if (!IsBlack(pl)) { bestB = pl[0]; bestG = pl[1]; bestR = pl[2]; found = true; break; }

                                    byte* pr = rr + x1 * 3;
                                    if (!IsBlack(pr)) { bestB = pr[0]; bestG = pr[1]; bestR = pr[2]; found = true; break; }
                                }
                            }
                        }

                        if (found)
                        {
                            p[0] = bestB;
                            p[1] = bestG;
                            p[2] = bestR;
                        }
                    }
                }
            }
            finally
            {
                bmp.UnlockBits(data);
            }
        }


        /// <summary>
        /// Заполняет "чёрные пустоты" (почти чёрные пиксели) ближайшими нормальными пикселями.
        /// Быстро работает через LockBits.
        /// </summary>









        static void FixVerticalSeams(Bitmap strip, int slotW, int seamHalfW = 6, int blendW = 4)
        {
            // seamHalfW: сколько пикселей берём слева/справа для оценки яркости
            // blendW: ширина мягкого перехода (можно 0, если вообще без смешивания)
            seamHalfW = Math.Max(1, seamHalfW);
            blendW = Math.Max(0, blendW);

            if (strip.PixelFormat != PixelFormat.Format24bppRgb)
                return;

            Rectangle rect = new Rectangle(0, 0, strip.Width, strip.Height);
            var data = strip.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            try
            {
                unsafe
                {
                    byte* basePtr = (byte*)data.Scan0;
                    int stride = data.Stride;
                    int H = strip.Height;
                    int W = strip.Width;

                    // Швы между 5 слотами: x = slotW, 2*slotW, 3*slotW, 4*slotW
                    for (int seam = 1; seam <= 4; seam++)
                    {
                        int x0 = seam * slotW; // граница

                        // ограничим, чтобы окна не вылезли за картинку
                        int leftStart = Math.Max(0, x0 - seamHalfW);
                        int leftEnd = Math.Min(W, x0);
                        int rightStart = Math.Max(0, x0);
                        int rightEnd = Math.Min(W, x0 + seamHalfW);

                        if (leftEnd - leftStart < 1 || rightEnd - rightStart < 1)
                            continue;

                        // 1) меряем среднюю яркость слева и справа (сэмплируем по Y)
                        double sumL = 0, sumR = 0;
                        long cntL = 0, cntR = 0;

                        int stepY = 4; // быстрее
                        int stepX = 2;

                        for (int y = 0; y < H; y += stepY)
                        {
                            byte* row = basePtr + y * stride;

                            for (int x = leftStart; x < leftEnd; x += stepX)
                            {
                                byte* p = row + x * 3; // BGR
                                int lum = (p[2] + p[1] + p[0]) / 3;
                                sumL += lum;
                                cntL++;
                            }

                            for (int x = rightStart; x < rightEnd; x += stepX)
                            {
                                byte* p = row + x * 3;
                                int lum = (p[2] + p[1] + p[0]) / 3;
                                sumR += lum;
                                cntR++;
                            }
                        }

                        if (cntL == 0 || cntR == 0) continue;

                        double meanL = sumL / cntL;
                        double meanR = sumR / cntR;
                        if (meanR < 1.0) meanR = 1.0;

                        // 2) коэффициент, чтобы правая сторона стала похожа на левую
                        double gain = meanL / meanR;

                        // ограничим, чтобы не “взрывать” картинку
                        if (gain < 0.6) gain = 0.6;
                        if (gain > 1.6) gain = 1.6;

                        // 3) применяем gain к правому окну (можно только к узкой зоне возле шва)
                        // Чтобы не менять всю камеру, корректируем только область справа от шва на seamHalfW + blendW
                        int corrStart = x0;
                        int corrEnd = Math.Min(W, x0 + seamHalfW + blendW);

                        for (int y = 0; y < H; y++)
                        {
                            byte* row = basePtr + y * stride;
                            for (int x = corrStart; x < corrEnd; x++)
                            {
                                byte* p = row + x * 3;
                                int b = (int)(p[0] * gain);
                                int g = (int)(p[1] * gain);
                                int r = (int)(p[2] * gain);

                                p[0] = (byte)Math.Clamp(b, 0, 255);
                                p[1] = (byte)Math.Clamp(g, 0, 255);
                                p[2] = (byte)Math.Clamp(r, 0, 255);
                            }
                        }

                        // 4) очень узкий blend прямо на шве (делает “единую” картинку)
                        if (blendW > 0)
                        {
                            int blendStart = Math.Max(0, x0 - blendW);
                            int blendEnd = Math.Min(W, x0 + blendW);

                            for (int y = 0; y < H; y++)
                            {
                                byte* row = basePtr + y * stride;

                                for (int x = blendStart; x < blendEnd; x++)
                                {
                                    float t = (x - blendStart) / (float)Math.Max(1, (blendEnd - blendStart - 1)); // 0..1

                                    // берём пиксель слева и справа от шва (по ближайшим координатам)
                                    int xl = Math.Max(0, Math.Min(W - 1, x0 - 1));
                                    int xr = Math.Max(0, Math.Min(W - 1, x0));

                                    byte* pl = row + xl * 3;
                                    byte* pr = row + xr * 3;
                                    byte* pd = row + x * 3;

                                    // линейное смешение (очень узко)
                                    pd[0] = (byte)Math.Clamp((int)(pl[0] * (1 - t) + pr[0] * t), 0, 255);
                                    pd[1] = (byte)Math.Clamp((int)(pl[1] * (1 - t) + pr[1] * t), 0, 255);
                                    pd[2] = (byte)Math.Clamp((int)(pl[2] * (1 - t) + pr[2] * t), 0, 255);
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                strip.UnlockBits(data);
            }
        }


        //private static void SaveFirstRowStripToTemp(List<Bitmap> firstFive, int W, int H)
        //{
        //    try
        //    {
        //        Directory.CreateDirectory(@"C:\temp");

        //        using var rowStrip = new Bitmap(W * 5, H, PixelFormat.Format24bppRgb);
        //        using var g = Graphics.FromImage(rowStrip);

        //        g.Clear(Color.Black);
        //        g.InterpolationMode = InterpolationMode.NearestNeighbor;
        //        g.SmoothingMode = SmoothingMode.None;
        //        g.CompositingMode = CompositingMode.SourceCopy;

        //        static Bitmap MirrorX(Bitmap src)
        //        {
        //            var c = (Bitmap)src.Clone();
        //            c.RotateFlip(RotateFlipType.RotateNoneFlipX);
        //            return c;
        //        }

        //        Bitmap[] prepared = new Bitmap[5];

        //        for (int j = 0; j < 5; j++)
        //            prepared[j] = MirrorX(firstFive[j]);

        //        for (int j = 0; j < 5; j++)
        //        {
        //            int visualSlot = 4 - j;
        //            int destX = visualSlot * W;

        //            g.DrawImage(
        //                prepared[j],
        //                new Rectangle(destX, 0, W, H),
        //                new Rectangle(0, 0, W, H),
        //                GraphicsUnit.Pixel
        //            );
        //        }

        //        rowStrip.Save(@"C:\temp\first_row_strip.png", ImageFormat.Png);

        //        for (int j = 0; j < 5; j++)
        //            prepared[j]?.Dispose();
        //    }
        //    catch
        //    {
        //        // ignore debug save errors
        //    }
        //}

        private static int FindBestDyForRowSeam(
    Bitmap canvas,
    int yCursor,
    Bitmap rowStrip,
    int rowOverlap,
    int searchDy)
        {
            // canvas: уже собранная картинка
            // yCursor: текущая Y-позиция, куда "ставим" rowStrip
            // rowStrip: новая полоса, которую добавляем
            // rowOverlap: высота перекрытия (сколько строк сравниваем)
            // searchDy: поиск смещения вверх/вниз в диапазоне [-searchDy..+searchDy]

            if (canvas == null || rowStrip == null) return 0;
            if (rowOverlap <= 0) return 0;

            // В canvas зона перекрытия заканчивается на yCursor (перед вставкой новой полосы)
            int canvasOverlapY0 = yCursor - rowOverlap;
            if (canvasOverlapY0 < 0) return 0;

            // Ограничим перекрытие, чтобы не выйти за границы
            int overlapH = Math.Min(rowOverlap, Math.Min(rowStrip.Height, yCursor));
            if (overlapH <= 0) return 0;

            int bestDy = 0;
            long bestScore = long.MaxValue;

            // Брутфорс по dy
            for (int dy = -searchDy; dy <= searchDy; dy++)
            {
                long score = 0;
                int count = 0;

                // Сравниваем пиксели в зоне перекрытия:
                // canvas: [canvasOverlapY0 .. canvasOverlapY0+overlapH)
                // rowStrip: [0+dy .. overlapH+dy)
                int rsY0 = 0 + dy;

                // Если dy увёл за пределы rowStrip — пропускаем лишнее
                int start = 0;
                int end = overlapH;

                if (rsY0 < 0) start = -rsY0;
                if (rsY0 + end > rowStrip.Height) end = rowStrip.Height - rsY0;
                if (start >= end) continue;

                for (int y = start; y < end; y++)
                {
                    int cy = canvasOverlapY0 + y;
                    int ry = rsY0 + y;

                    if (cy < 0 || cy >= canvas.Height) continue;
                    if (ry < 0 || ry >= rowStrip.Height) continue;

                    // Можно ускорить через LockBits, но для начала так проще.
                    for (int x = 0; x < rowStrip.Width && x < canvas.Width; x++)
                    {
                        Color c1 = canvas.GetPixel(x, cy);
                        Color c2 = rowStrip.GetPixel(x, ry);

                        // Метрика: сумма |ΔR|+|ΔG|+|ΔB|
                        score += Math.Abs(c1.R - c2.R);
                        score += Math.Abs(c1.G - c2.G);
                        score += Math.Abs(c1.B - c2.B);
                        count++;
                    }

                    // лёгкая защита от переполнения/долгих циклов
                    if (score > bestScore) break;
                }

                if (count == 0) continue;

                // Нормировка по количеству сравнений (чтобы dy с меньшим overlap не выигрывал “случайно”)
                long normScore = score / Math.Max(1, count);

                if (normScore < bestScore)
                {
                    bestScore = normScore;
                    bestDy = dy;
                }
            }

            return bestDy;
        }












        static void ApplyBrightnessGain(Bitmap bmp, float gain)
        {
            if (Math.Abs(gain - 1f) < 0.001f) return;

            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            var bd = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            try
            {
                unsafe
                {
                    byte* p0 = (byte*)bd.Scan0;
                    for (int y = 0; y < bmp.Height; y++)
                    {
                        byte* p = p0 + y * bd.Stride;
                        for (int x = 0; x < bmp.Width; x++)
                        {
                            byte* px = p + x * 3;
                            px[0] = (byte)Math.Clamp(px[0] * gain, 0, 255); // B
                            px[1] = (byte)Math.Clamp(px[1] * gain, 0, 255); // G
                            px[2] = (byte)Math.Clamp(px[2] * gain, 0, 255); // R
                        }
                    }
                }
            }
            finally
            {
                bmp.UnlockBits(bd);
            }
        }

        static void ApplyGamma(Bitmap bmp, float gamma)
        {
            if (Math.Abs(gamma - 1f) < 0.001f) return;

            // gamma < 1 => светлее (например 0.85)
            // gamma > 1 => темнее
            byte[] lut = new byte[256];
            for (int i = 0; i < 256; i++)
            {
                int v = (int)Math.Round(Math.Pow(i / 255.0, 1.0 / gamma) * 255.0);
                lut[i] = (byte)Math.Clamp(v, 0, 255);
            }

            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            var bd = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            try
            {
                unsafe
                {
                    byte* p0 = (byte*)bd.Scan0;
                    for (int y = 0; y < bmp.Height; y++)
                    {
                        byte* p = p0 + y * bd.Stride;
                        for (int x = 0; x < bmp.Width; x++)
                        {
                            byte* px = p + x * 3;
                            px[0] = lut[px[0]];
                            px[1] = lut[px[1]];
                            px[2] = lut[px[2]];
                        }
                    }
                }
            }
            finally
            {
                bmp.UnlockBits(bd);
            }
        }


        private async Task OnKmChangedHandler(int v)
        {
            // пользователь явно указал км → фиксируем приоритет
            _userKmPinned = true;
            _userKmNumber = v;

            Number = v;
            SelectKilometer(Number, resetPosition: true);
            Console.WriteLine($"[PLAY-AFTER-SELECT] pinned={_userKmPinned}, Number={Number}, CurrentKm={CurrentKm?.Number}");

            SyncHudFromState();
            StateHasChanged();
            await Task.CompletedTask;
        }


        // Безопасный кламп значений
        private static int Clamp(int v, int min, int max) => v < min ? min : (v > max ? max : v);
        // UI-выбор PKM
        private int _selectedPicket = 1; // 1..10      


        // Установить позицию по пикету/метру
        private void SetByPKM(int picket, int meter, bool redraw = true)
        {
            // Пикет: 1..10 (обычно 10 пикетов по 100м в км), Метр: 0..99
            picket = Clamp(picket, 1, 10);
            meter = Clamp(meter, 0, 99);

            int targetMeter = (picket - 1) * 100 + meter;

            // Если у км есть нестандартный Start_m — считаем относительно него
            // StartMeter — начало текущего км
            CurrentMeter = targetMeter;

            // Преобразование в кадр/время (у тебя 5 кадров на метр, 200ms на кадр)
            int deltaMeters = CurrentMeter - StartMeter;
            if (deltaMeters < 0) deltaMeters = 0;

            CurrentVideoFrame = deltaMeters * 5;
            CurrentMs = CurrentVideoFrame * 200; // = deltaMeters * 1000

            // Обновим HUD и перерисуем картинку
            SyncHudFromState();
            if (FileIdList != null && FileIdList.Count > 0 && redraw)
            {
                GetImage2(FileIdList[0]);
                StateHasChanged();
            }
        }

        // Обработчики изменения полей в UI
        private Task OnPicketChanged(int v)
        {
            _selectedPicket = v;
            SetByPKM(_selectedPicket, _selectedMeter);
            return Task.CompletedTask;
        }

        private int _selectedMeter = 0; // 0..999

        private Task OnMeterChanged(int v)
        {
            _selectedMeter = Clamp(v, 0, 999);
            CurrentMeter = _selectedMeter;
            // дальше пересчёт CurrentVideoFrame / CurrentMs
            return Task.CompletedTask;
        }
        private int _meterInput = 0;
        private Task OnMeterInputChanged(int v)
        {
            var abs = Clamp(v, 0, 999);
            _meterInput = abs;

            // разложить 0..999 в ПК(1..10) и метр(0..99)
            _selectedPicket = abs / 100 + 1;
            _selectedMeter = abs % 100;

            // выставить позицию (кадр/время перессчитаются)
            SetByPKM(_selectedPicket, _selectedMeter);
            return Task.CompletedTask;
        }
        private void SyncPKMFromCurrentMeter()
        {
            _selectedPicket = (CurrentMeter / 100) + 1;
            _selectedMeter = CurrentMeter % 100;
            _meterInput = Clamp(CurrentMeter, 0, 999);
        }



        //public async Task OnTimedEventAsync() => await TogglePlayPauseAsync();
        // 1) Обработчик смены километра из UI (правой панели)
        public void OnKmChanged(int v)
        {
            _userKmPinned = true;
            _userKmNumber = v;

            Number = v;
            SelectKilometer(Number, resetPosition: true);
            SyncHudFromState();
            StateHasChanged();
        }

        // 2) Хелпер выбора километра
        private void SelectKilometer(int kmNumber, bool resetPosition)
        {
            if (Kilometers is { Count: > 0 })
            {
                var desired = Kilometers.FirstOrDefault(k => k.Number == kmNumber) ?? Kilometers.First();
                bool kmChanged = CurrentKm == null || desired.Number != CurrentKm.Number;

                CurrentKm = desired;
                Number = CurrentKm.Number;

                if (resetPosition && kmChanged)
                {
                    StartMeter = CurrentKm.Start_m;
                    CurrentMeter = StartMeter;
                    CurrentVideoFrame = 0;
                    CurrentMs = 0;
                }
            }
        }


        private volatile bool _frameBusy = false;
        private volatile bool _stopRequested = false;
        // 3) Полная замена старого метода — старт/пауза с продолжением

        // Применяем km/meter/direction к состоянию без автоплея
        private void ApplySelectionFromAppData(int km, int meter)
        {
            // выбрать километр и сброситься на его начало
            SelectKilometer(km, resetPosition: true);

            // разложить общий метр 0..999 в пикет/метр
            var picket = (meter / 100) + 1;
            var meterInPicket = meter % 100;

            // поставить позицию (кадр/время пересчитаются), перерисовку отключим — покажем кадр ниже
            SetByPKM(picket, meterInPicket, redraw: false);

            SyncHudFromState();
        }
        // Вернёт первый fileId для данного км, если есть
        private bool TryGetFileIdForKm(int kmNumber, out long fileId)
        {
            fileId = 0;
            try
            {
                var list = AppData?.RdStructureRepository?.GetFileID(AppData.Trip.Id, kmNumber);
                if (list != null && list.Count > 0)
                {
                    fileId = list[0];
                    return true;
                }
            }
            catch { /* ignore */ }
            return false;
        }

        // Ищем ближайший км к startKm, для которого есть файлы.
        // Идём радиусом 0,1,1,2,2... по индексам в отсортированном списке километров.
        private bool TryFindNearestKmWithFiles(int startKm, out int foundKm, out long fileId)
        {
            foundKm = -1; fileId = 0;

            if (Kilometers == null || Kilometers.Count == 0)
                return false;

            // Отсортируем по номеру км
            var ordered = Kilometers.OrderBy(k => k.Number).ToList();

            // Индекс стартового км (если нет — начнём с ближайшего по порядку)
            int idx = ordered.FindIndex(k => k.Number == startKm);
            if (idx < 0)
            {
                // Встанем в ближайшую позицию по номеру
                idx = ordered.BinarySearch(new Kilometer { Number = startKm }, Comparer<Kilometer>.Create((a, b) => a.Number.CompareTo(b.Number)));
                if (idx < 0) idx = ~idx; // место вставки
                idx = Math.Clamp(idx, 0, ordered.Count - 1);
            }

            // Радиусный поиск: текущий, вправо, влево, ++
            for (int radius = 0; radius < ordered.Count; radius++)
            {
                // кандидаты индексов на этом радиусе
                var candidates = new List<int>();
                int iRight = idx + radius;
                int iLeft = idx - radius;
                if (radius == 0)
                {
                    candidates.Add(idx);
                }
                else
                {
                    if (iRight < ordered.Count) candidates.Add(iRight);
                    if (iLeft >= 0) candidates.Add(iLeft);
                }

                foreach (var ci in candidates.Distinct())
                {
                    int km = ordered[ci].Number;
                    if (TryGetFileIdForKm(km, out var fid))
                    {
                        foundKm = km;
                        fileId = fid;
                        return true;
                    }
                }
            }

            return false;
        }
        // Показать один кадр по текущим полям (CurrentKm, CurrentMeter, CurrentVideoFrame/CurrentMs)

        private async Task ShowSingleFrameForSelectionAsync()
        {
            try
            {
                // гарантированно стоим на паузе
                AppData.VideoProcessing = false;
                try { _playCts?.Cancel(); } catch { /* ignore */ }
                try { _rewindCts?.Cancel(); } catch { /* ignore */ }

                if (CurrentKm == null)
                    return;

                // пересчёт кадра/времени от начала км
                StartMeter = CurrentKm.Start_m;
                var deltaMeters = Math.Max(0, CurrentMeter - StartMeter);
                CurrentVideoFrame = deltaMeters * 5;   // 5 кадров на метр
                CurrentMs = CurrentVideoFrame * 200;

                // достаём fileId для этого км
                var fileIds = AppData.RdStructureRepository.GetFileID(AppData.Trip.Id, CurrentKm.Number);
                if (fileIds == null || fileIds.Count == 0)
                {
                    // Toaster?.Add($"Нет файлов для км {CurrentKm.Number}", MatToastType.Warning);
                    Base64 = null;
                    await InvokeAsync(StateHasChanged);
                    return;
                }

                // покажем первый (при желании позже сделаем выбор по метру)
                GetImage2(fileIds[0]);
                await InvokeAsync(StateHasChanged);
            }
            catch { /* тихо игнорим, как у тебя принято */ }
        }
        // Пользовательский выбор километра (приоритет)
        private bool _userKmPinned = false;
        private int _userKmNumber = 0;

        // когда надо “разрешить” автопоиск (если захочешь иногда включать)
        private bool AllowAutoJumpKm => !_userKmPinned;
        public async Task OnTimedEventAsync()
        {
            // Локальные хелперы (чтобы не править весь класс)
            bool TryGetFileIdForKm(int kmNumber, out long fileId)
            {
                fileId = 0;
                try
                {
                    var list = AppData?.RdStructureRepository?.GetFileID(AppData.Trip.Id, kmNumber);
                    if (list != null && list.Count > 0)
                    {
                        fileId = list[0];
                        return true;
                    }
                }
                catch { /* ignore */ }
                return false;
            }

            bool TryFindNearestKmWithFiles(int startKm, out int foundKm, out long foundFileId)
            {
                foundKm = -1; foundFileId = 0;

                if (Kilometers == null || Kilometers.Count == 0)
                    return false;

                var ordered = Kilometers.OrderBy(k => k.Number).ToList();

                // Индекс стартового км (если нет — ближайшая позиция)
                int idx = ordered.FindIndex(k => k.Number == startKm);
                if (idx < 0)
                {
                    idx = ordered.BinarySearch(new Kilometer { Number = startKm },
                        Comparer<Kilometer>.Create((a, b) => a.Number.CompareTo(b.Number)));
                    if (idx < 0) idx = ~idx;
                    idx = Math.Clamp(idx, 0, ordered.Count - 1);
                }

                // Радиусный обход: 0, +1, -1, +2, -2, ...
                for (int radius = 0; radius < ordered.Count; radius++)
                {
                    if (radius == 0)
                    {
                        int km = ordered[idx].Number;
                        if (TryGetFileIdForKm(km, out var fid)) { foundKm = km; foundFileId = fid; return true; }
                    }
                    else
                    {
                        int r = idx + radius, l = idx - radius;

                        if (r < ordered.Count)
                        {
                            int km = ordered[r].Number;
                            if (TryGetFileIdForKm(km, out var fid)) { foundKm = km; foundFileId = fid; return true; }
                        }
                        if (l >= 0)
                        {
                            int km = ordered[l].Number;
                            if (TryGetFileIdForKm(km, out var fid)) { foundKm = km; foundFileId = fid; return true; }
                        }
                    }
                }
                return false;
            }

            try
            {
                // ── Тоггл: если не играем — запускаем; если играем — мягкая пауза
                bool starting = !AppData.VideoProcessing;

                if (!starting)
                {
                    AppData.VideoProcessing = false;
                    _stopRequested = true;

                    var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(2);
                    while (_frameBusy && DateTime.UtcNow < deadline)
                        await Task.Delay(10);

                    Console.WriteLine("⏸ Пауза запрошена (после кадра).");
                    return;
                }

                // СТАРТ
                _stopRequested = false;
                AppData.VideoProcessing = true;

                if (Kilometers == null || Kilometers.Count == 0)
                {
                    Console.WriteLine("[WARN] Нет километров.");
                    AppData.VideoProcessing = false;
                    return;
                }

                if (_userKmPinned)
                    Number = _userKmNumber;

                var selectedKm = Kilometers.FirstOrDefault(km => km.Number == Number);

                if (selectedKm == null)
                {
                    // если км задан пользователем — НЕ прыгаем на 6215
                    if (_userKmPinned)
                    {
                        AppData.VideoProcessing = false;
                        Base64 = null;
                        try
                        {
                            Toaster?.Add($"Километр {Number} отсутствует в списке (нет данных/не загружен).",
                                MatBlazor.MatToastType.Warning, "Просмотр видео");
                        }
                        catch { }
                        await InvokeAsync(StateHasChanged);
                        return;
                    }

                    // только если НЕ pinned — старое поведение
                    selectedKm = Kilometers.First();
                }


                bool kmChanged = CurrentKm == null || CurrentKm.Number != selectedKm.Number;
                if (kmChanged || CurrentKm == null)
                {
                    CurrentKm = selectedKm;
                    Number = CurrentKm.Number;
                    StartMeter = CurrentKm.Start_m;
                    CurrentMeter = StartMeter;
                    CurrentVideoFrame = 0;
                    CurrentMs = 0;
                }

                if (N_rows == 0) N_rows = 5;

                Console.WriteLine($"▶ Старт: км {CurrentKm.Number}, кадр {CurrentVideoFrame}, ms={CurrentMs}");

                // Основной цикл воспроизведения (мягкая пауза — после кадра)
                while (true)
                {
                    if (!AppData.VideoProcessing && !_stopRequested)
                        break;

                    SyncHudFromState();

                    // --- Подбор файла (с автопрыжком на ближайший км с файлами) ---
                    long fileIdToUse;
                    var fileIdList = AppData.RdStructureRepository.GetFileID(AppData.Trip.Id, CurrentKm.Number);

                    if (fileIdList == null || fileIdList.Count == 0)
                    {
                        Console.WriteLine($"[WARN] Нет файлов для км {CurrentKm.Number}.");

                        // ✅ если км задан пользователем — НЕ прыгаем никуда
                        if (_userKmPinned)
                        {
                            AppData.VideoProcessing = false;
                            _stopRequested = false;

                            Base64 = null;
                            try
                            {
                                Toaster?.Add($"Нет кадров для выбранного километра {CurrentKm.Number}",
                                    MatBlazor.MatToastType.Warning, "Просмотр видео");
                            }
                            catch { }

                            try { await InvokeAsync(StateHasChanged); } catch { }
                            break;
                        }

                        // иначе (если НЕ прибит пользователем) — можно как раньше искать ближайший
                        Console.WriteLine($"[INFO] Автопоиск ближайшего км с файлами…");

                        if (TryFindNearestKmWithFiles(CurrentKm.Number, out var foundKm, out var foundFileId))
                        {
                            SelectKilometer(foundKm, resetPosition: true);
                            StartMeter = CurrentKm.Start_m;
                            CurrentMeter = StartMeter;
                            CurrentVideoFrame = 0;
                            CurrentMs = 0;
                            SyncHudFromState();

                            fileIdToUse = foundFileId;
                            Console.WriteLine($"[INFO] Переключился на км {foundKm}, fileId={fileIdToUse}");
                        }
                        else
                        {
                            AppData.VideoProcessing = false;
                            Base64 = null;
                            try { Toaster?.Add("Нет кадров для выбранного и соседних километров", MatBlazor.MatToastType.Warning, "Просмотр видео"); } catch { }
                            try { await InvokeAsync(StateHasChanged); } catch { }
                            break;
                        }
                    }
                    else
                    {
                        fileIdToUse = fileIdList[0];
                    }


                    // --- Рендер кадра (критическая секция) ---
                    _frameBusy = true;
                    try
                    {
                        GetImage2(fileIdToUse);
                    }
                    finally
                    {
                        _frameBusy = false;
                    }

                    // Обновим UI
                    try { await InvokeAsync(StateHasChanged); } catch { /* ignore */ }

                    // Мягкая пауза (если нажали во время кадра)
                    if (_stopRequested || !AppData.VideoProcessing)
                    {
                        _stopRequested = false;
                        break;
                    }

                    // Задержка и продвижение времени/кадра
                    try { await Task.Delay(AppData.Speed); } catch { /* ignore */ }

                    CurrentVideoFrame += PlaybackSpeed;
                    CurrentMs += FrameDurationMs * PlaybackSpeed;
                    CurrentMeter = StartMeter + (CurrentVideoFrame / FramesPerMeter);
                }

                Console.WriteLine("⏹ Остановлено.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] OnTimedEventAsync: {ex.Message}");
                Console.WriteLine($"[TRACE] {ex.StackTrace}");
                // Страховочные сбросы флагов
                _frameBusy = false;
                _stopRequested = false;
                AppData.VideoProcessing = false;
            }
        }


        private CancellationTokenSource _playCts;
        private Task _playTask;
        //private bool IsPlaying => AppData?.VideoProcessing == true;
        //public async Task TogglePlayPauseAsync()
        //{
        //    if (IsPlaying)
        //    {
        //        PauseAsync();
        //    }
        //    else
        //    {
        //        await PlayAsync();
        //    }
        //}
        private bool ReverseActive => _rewindCts != null && !_rewindCts.IsCancellationRequested;
        private bool IsPlaying => AppData?.VideoProcessing == true;

        public async Task TogglePlayPauseAsync()
        {
            if (IsPlaying || ReverseActive)
            {
                await PauseAsync(); // стоп и прямого, и реверса
            }
            else
            {
                await PlayAsync();
            }
        }

        // «Старт» прямого — гасит реверс
        //public async Task PlayAsync()
        //{
        //    // ✅ если пользователь зафиксировал км — держим его
        //    if (_userKmPinned)
        //        Number = _userKmNumber;

        //    // ✅ синхронизируем CurrentKm с Number
        //    if (!SelectKilometer(Number, resetPosition: false, allowFallback: !_userKmPinned))
        //    {
        //        // pinned км отсутствует в списке — не прыгаем на 6215
        //        AppData.VideoProcessing = false;
        //        Base64 = null;
        //        try
        //        {
        //            Toaster?.Add($"Километр {Number} отсутствует в списке", MatBlazor.MatToastType.Warning, "Просмотр видео");
        //        }
        //        catch { }
        //        await InvokeAsync(StateHasChanged);
        //        return;
        //    }

        //    // ✅ фиксируем стартовый метр
        //    StartMeter = CurrentKm.Start_m;

        //}
        public async Task PlayAsync()
        {
            if (IsPlaying) return;

            // ⛔ стоп реверса
            try { _rewindCts?.Cancel(); } catch { }
            _rewindCts?.Dispose();
            _rewindCts = null;

            _stopRequested = false;

            try { _playCts?.Cancel(); } catch { }
            _playCts?.Dispose();

            _playCts = new CancellationTokenSource();
            AppData.VideoProcessing = true;

            // ▶ старт реального цикла
            _playTask = RunLoopAsync(_playCts.Token);

            await Task.CompletedTask;
        }


        public async Task ToggleReverseAsync()
        {
            if (ReverseActive)
                await StopReverse();        // ← обязательно await
            else
                await StartReverseAsync();
        }
        // === Граница метра (красная линия) ===
        private int HudLineY = 0;         // экранная Y-позиция линии в пикселях
        private bool ShowHudLine = false;  // показывать ли линию на этом кадре
        private const int FrameDurationMs = 200; // 200 мс/кадр
        private const int FramesPerMeter = 5;   // 5 кадров = 1 метр


        private async Task RunLoopAsync(CancellationToken token)
        {
            try
            {
                while (IsPlaying && !token.IsCancellationRequested)
                {
                    // Актуализируем HUD (номер км/пикета/метра)
                    SyncHudFromState();
                    if (CurrentKm == null)
                    {
                        return;
                    }
                    // Получаем файл для текущего километра
                    var fileIdList = AppData.RdStructureRepository.GetFileID(AppData.Trip.Id, CurrentKm.Number);
                    if (fileIdList == null || fileIdList.Count == 0)
                    {
                        Console.WriteLine("[WARN] Нет файлов для текущего километра.");
                        break;
                    }

                    // Рендер кадра
                    GetImage2(fileIdList[0]);

                    // Перерисовать UI (безопасно из фонового потока)
                    try { await InvokeAsync(StateHasChanged); } catch { /* ignore */ }

                    // Ждём с учётом скорости/паузы/отмены
                    try
                    {
                        await Task.Delay(AppData.Speed, token);
                        if (TotalFrames > 0 && CurrentVideoFrame >= TotalFrames - 1)
                        {
                            Console.WriteLine("[INFO] Конец видео. Останавливаем цикл.");
                            await PauseAsync();
                            break;
                        }

                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                    // 🚨 Проверка конца
                    if (TotalFrames > 0 && CurrentVideoFrame >= TotalFrames - 1)
                    {
                        Console.WriteLine("[INFO] Конец видео. Останавливаем цикл.");
                        await PauseAsync();          // <-- корректно ставит паузу, гасит токен/флаг
                        break;                       // выходим из while
                    }

                    // Двигаемся дальше — БЕЗ СБРОСА (важно для возобновления)
                    CurrentVideoFrame++;
                    CurrentMs += 200;
                    CurrentMeter = StartMeter + (CurrentVideoFrame / 5);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] RunLoopAsync: {ex.Message}");
            }
            finally
            {
                // Если вышли из цикла не по паузе — сброс состояния
                if (!token.IsCancellationRequested)
                    AppData.VideoProcessing = false;

                try { await InvokeAsync(StateHasChanged); } catch { /* ignore */ }
            }
        }


        [Inject] IJSRuntime JS { get; set; } = default!;

        ElementReference ViewerWrapper;
        DotNetObjectReference<Video>? objRef;
        bool IsFullscreen;

        async Task ToggleFullScreen()
        {
            try
            {
                if (IsFullscreen)
                    await JS.InvokeVoidAsync("viewer.exitFullscreen");
                else
                    await JS.InvokeVoidAsync("viewer.enterFullscreen", ViewerWrapper);
            }
            catch (JSException ex)
            {
                Toaster?.Add($"Fullscreen: {ex.Message}", MatBlazor.MatToastType.Warning);
            }

            // сразу обновим флаг и вернём фокус, чтобы хоткеи работали в FS
            IsFullscreen = await IsFullscreenActiveSafeAsync();
            await FocusViewer();
        }



        private bool _disposed;

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;

            // 1) Остановить любые фоновые процессы воспроизведения
            try
            {
                AppData.VideoProcessing = false;

                try { _playCts?.Cancel(); } catch { /* ignore */ }
                _playCts?.Dispose();
                _playCts = null;

                try { _rewindCts?.Cancel(); } catch { /* ignore */ }
                _rewindCts?.Dispose();
                _rewindCts = null;
            }
            catch { /* ignore */ }

            // 2) Снять JS-подписки (порядок: клавиши → fullscreen)
            try { await JS.InvokeVoidAsync("viewer.unbindGlobalKeys"); } catch { /* ignore */ }
            try { await JS.InvokeVoidAsync("viewer.offFullscreenChange"); } catch { /* ignore */ }

            // 3) Освободить DotNetObjectReference
            try
            {
                objRef?.Dispose();
                objRef = null;
            }
            catch { /* ignore */ }
        }


        private bool IsFullScreen = false;
        private bool IsFullScreenHudVisible = false;



        private int UiKm, UiPicket, UiMeter, CarPos;
        private string UiPch, UiPd, UiPdp, UiDirectionName, UiDirectionCode, Chiefname, Naprav;
        private void FillPchPdPdbFromSection()
        {
            // пример строки: "ПЧ-21/ПЧУ-1/ПД-1/ПДБ-1"
            var section = AppData?.Data?[(int)Services.Series.Section]?.ToString();
            if (string.IsNullOrWhiteSpace(section)) return;

            // вытащим части регэкспами — надёжно независимо от порядка и пробелов
            var mch = Regex.Match(section, @"ПЧ[^/]*", RegexOptions.IgnoreCase);
            var mpd = Regex.Match(section, @"ПД(?!Б)[^/]*", RegexOptions.IgnoreCase);   // ПД, но не ПДБ
            var mpdb = Regex.Match(section, @"ПДБ[^/]*", RegexOptions.IgnoreCase);

            if (mch.Success) UiPch = mch.Value.Trim();
            if (mpd.Success) UiPd = mpd.Value.Trim();
            if (mpdb.Success) UiPdp = mpdb.Value.Trim();
        }
        public string UiCarPos { get; set; } = "";
        public string UiNaprav { get; set; } = "";

        private void SyncHudFromState()
        {
            // данные по текущему километру
            var pdb = CurrentKm;
            if (pdb != null)
            {
                UiDirectionName = pdb.Direction_name;
                UiDirectionCode = pdb.Direction_code;
                Chiefname = pdb.PdChief;
                Naprav = pdb.Direction.ToString(); // оставляем «сырой» enum/значение если нужно
                CarPos = (int)AppData.Trip.Car_Position;
            }

            // ПЧ/ПД/ПДБ — берём из секции (как в TrackPanel)
            FillPchPdPdbFromSection();

            // позиция
            UiKm = Number;
            UiPicket = (CurrentMeter / 100) + 1;
            UiMeter = CurrentMeter % 100;

            // если ведёшь ещё поле 0..999, синхронизируй здесь
            SyncPKMFromCurrentMeter();

            // читаемые подписи:
            UiCarPos = CarPos switch
            {
                1 => "вперёд",
                -1 => "назад",
                _ => $"CarPos={CarPos}" // fallback, если вдруг другое значение
            };

            UiNaprav = Naprav?.ToLower() switch
            {
                "direct" => "Прямой",
                "reverse" => "Обратный",
                _ => Naprav ?? ""
            };
        }


        public string[] CamLabels { get; private set; } = Array.Empty<string>();

        private void UpdateCamLabels(List<Bitmap> frames)
        {
            var labels = new List<string>();

            for (int i = 0; i < frames.Count; i++)
            {
                if (frames[i] != null)
                {
                    string label = i switch
                    {
                        //0 => "Правая • кадр 0",
                        //2 => "Центр • кадр 2",
                        //4 => "Левая • кадр 4",
                        //_ => $"Камера {i}"
                        0 => "-",
                        2 => "-",
                        4 => "-",
                        _ => $"- {i}"
                    };
                    labels.Add(label);
                }
            }

            CamLabels = labels.ToArray();
        }



        //}
        void GetObjectsFromFrame()
        {
            try
            {
                Gaps = CurrentKm.Gaps.Where(o => o.Meter == CurrentMeter).ToList();
                Fasteners = CurrentKm.Fasteners.Where(o => o.Meter == CurrentMeter).ToList();
                Bolts = CurrentKm.Bolts.Where(o => o.Meter == CurrentMeter).ToList();
                DefShpals = CurrentKm.DefShpals.Where(o => o.Meter == CurrentMeter).ToList();
                PerShpals = CurrentKm.PerShpals.Where(o => o.Meter == CurrentMeter).ToList();
                ObjectsDialog = true;
            }
            catch (Exception e)
            {
                Toaster.Add($"Отсутствуют данные по указанному километру", MatBlazor.MatToastType.Warning, "Просмотр видео проезда");
            }

        }
        public Task NextClick() => StepFramesAsync(+1);
        public Task PrevClick() => StepFramesAsync(-1);

        // 4) (Опционально) Автопауза при ручном шаге — удобно, чтобы цикл не «перебивал» кадры
        //public void NextClick()
        //{
        //    AppData.VideoProcessing = false; // автопауза
        //    CurrentMs += 200;
        //    CurrentVideoFrame += 1;
        //    SyncHudFromState();
        //    if (FileIdList != null && FileIdList.Count > 0)
        //    {
        //        GetImage2(FileIdList[0]);
        //        StateHasChanged();
        //    }
        //}

        //public void PrevClick()
        //{
        //    AppData.VideoProcessing = false; // автопауза
        //    CurrentMs -= 200;
        //    CurrentVideoFrame -= 1;
        //    SyncHudFromState();
        //    if (FileIdList != null && FileIdList.Count > 0)
        //    {
        //        GetImage2(FileIdList[0]);
        //        StateHasChanged();
        //    }
        //}
        // 5) Кнопка «Обновить» — сбросить позицию на начало текущего Number
        //void RestartKm()
        //{
        //    SelectKilometer(Number, resetPosition: true);
        //    SyncHudFromState();
        //    if (FileIdList != null && FileIdList.Count > 0)
        //    {
        //        GetImage2(FileIdList[0]);
        //        StateHasChanged();
        //    }
        //}
        // ===== 1) ссылка на контейнер хоткеев (совпадает с @ref из Razor) =====
        private ElementReference _hotkeysHost;

        // ===== 2) шаг и его логика =====     
        private int _frameStepOverride = 0;      // 0 => использовать N_rows
        public int FrameStepOverride
        {
            get => _frameStepOverride;
            set => _frameStepOverride = Math.Max(0, value);
        }
        private int EffectiveFrameStep => Math.Max(1, FrameStepOverride > 0 ? FrameStepOverride : N_rows);

        // ===== 3) методы шага =====
        public async Task StepNext() => await StepFramesAsync(+EffectiveFrameStep);
        public async Task StepPrev() => await StepFramesAsync(-EffectiveFrameStep);
        // ----- Обёртки для кнопок (void) -----
        private void StepPrevHandler() => _ = StepPrev();
        private void StepNextHandler() => _ = StepNext();
        // поля класса Video
        private bool _stepLocked;
        private int TotalFrames = 0; // если знаешь — заполни реальным значением

        private async Task StepFramesAsync(int delta)
        {
            if (delta == 0) return;

            if (IsPlaying) await PauseAsync();
            if (IsRewinding) await StopReverse();

            if (_stepLocked) return;
            _stepLocked = true;
            try
            {
                var newFrame = CurrentVideoFrame + delta;
                if (newFrame < 0) newFrame = 0;
                if (TotalFrames > 0 && newFrame >= TotalFrames) newFrame = TotalFrames - 1;
                if (newFrame == CurrentVideoFrame) return;

                CurrentVideoFrame = newFrame;
                CurrentMs = CurrentVideoFrame * FrameDurationMs;
                CurrentMeter = StartMeter + (CurrentVideoFrame / FramesPerMeter);

                // fileId на всякий случай
                if (CurrentFileId == 0)
                {
                    var list = AppData.RdStructureRepository.GetFileID(AppData.Trip.Id, CurrentKm.Number);
                    if (list != null && list.Count > 0) CurrentFileId = list[0];
                }

                GetImage2(CurrentFileId);

                await InvokeAsync(() =>
                {
                    SyncHudFromState();
                    StateHasChanged();
                });
            }
            finally
            {
                await Task.Delay(80);
                _stepLocked = false;
            }
        }


        private bool _keyLocked = false;

        private async Task HandleKeyDown(KeyboardEventArgs e)
        {
            // Защита от авто-повтора клавиши (Windows держит ArrowDown — шлёт поток событий)
            if (e.Repeat || _keyLocked)
                return;

            _keyLocked = true;

            try
            {
                switch (e.Key)
                {
                    case "ArrowUp":
                        if (IsPlaying) await PauseAsync();   // стоп воспроизведение
                        await StepPrev();
                        break;

                    case "ArrowDown":
                        if (IsPlaying) await PauseAsync();
                        await StepNext();
                        break;

                    case " ":
                    case "Spacebar":
                        await TogglePlayPause();
                        break;
                }
            }
            finally
            {
                // Небольшая пауза, чтобы не накапливались события (анти-дребезг)
                await Task.Delay(100);
                _keyLocked = false;
            }
        }



        // ===== 5) автофокус контейнера (чтобы хоткеи работали сразу) =====
        protected override async Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();

            if (Kilometers is { Count: > 0 })
            {
                if (_pendingHas)
                {
                    AppData.VideoProcessing = false;
                    ApplySelectionFromAppData(_pendingKm, _pendingMeter);
                    await ShowSingleFrameForSelectionAsync();
                    _pendingHas = false;
                    return;
                }

                if (AppData.TryConsumeVideoSelection(out var km, out var meter))
                {
                    AppData.VideoProcessing = false;
                    ApplySelectionFromAppData(km, meter);
                    await ShowSingleFrameForSelectionAsync();
                    return;
                }
            }
        }



        private bool _hasPendingSelection = false;
        // буфер выбора, если он пришёл раньше, чем Kilometers
        private bool _pendingHas;
        private int _pendingKm, _pendingMeter;

        private bool _hasSelectionMarker;
        private int _markerKm, _markerMeter;
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (N_rows < 1) N_rows = 1;

            if (firstRender)
            {
                // автофокус
                try { await ViewerWrapper.FocusAsync(); } catch { /* ignore */ }

                // objRef один раз
                if (objRef is null)
                    objRef = DotNetObjectReference.Create(this);

                // ПОДСТРАХОВКА: если выбор уже положили в AppData до прихода параметров,
                // просто сохраним его в локальный буфер (без рендера/старта)
                if (!_userKmPinned && !_pendingHas && AppData.TryConsumeVideoSelection(out var km, out var meter))
                {
                    _pendingKm = km;
                    _pendingMeter = meter;
                    _pendingHas = true;
                }

                // fullscreen callbacks
                try { await JS.InvokeVoidAsync("viewer.onFullscreenChange", objRef); } catch { /* ignore */ }

                // глобальные хоткеи
                try
                {
                    try { await JS.InvokeVoidAsync("viewer.unbindGlobalKeys"); } catch { /* ignore */ }
                    await JS.InvokeVoidAsync("viewer.bindGlobalKeys", objRef);
                }
                catch { /* ignore */ }
            }

            await base.OnAfterRenderAsync(firstRender);
        }




        // ===== 6) toggle Play/Pause (используй свои PlayAsync/PauseAsync) =====
        private async Task TogglePlayPause()
        {
            if (IsPlaying)
                await PauseAsync();
            else
                await PlayAsync();
        }


        private void StopAllPlayback()
        {
            AppData.VideoProcessing = false;
            _stopRequested = false;

            try { _playCts?.Cancel(); } catch { /* ignore */ }
            try { _rewindCts?.Cancel(); } catch { /* ignore */ }
        }



        // Пауза должна останавливать всё
        public Task PauseAsync()
        {
            // стоп обычного воспроизведения
            AppData.VideoProcessing = false;
            try { _playCts?.Cancel(); } catch { /* ignore */ }

            // стоп реверса
            try { _rewindCts?.Cancel(); } catch { /* ignore */ }

            _stopRequested = false;
            return Task.CompletedTask;
        }


        // обёртка для событий
        private void TogglePlayPauseHandler()
        {
            _ = TogglePlayPause(); // запускаем без ожидания
        }
        // клик по окну просмотра — вернуть фокус (чтобы сразу работали стрелки/пробел)
        private async Task FocusViewer()
        {
            try { await ViewerWrapper.FocusAsync(); } catch { /* ignore */ }
        }

        // универсальный апдейтер fullscreen-состояния (работает как с параметром, так и без)
        //private async Task UpdateFullscreenStateAsync(bool? isFs)
        //{
        //    IsFullscreen = isFs ?? await IsFullscreenActiveSafeAsync();
        //    await FocusViewer();                 // чтобы хоткеи сразу работали после входа/выхода из FS
        //    await InvokeAsync(StateHasChanged);  // обновим UI
        //}
        private async Task UpdateFullscreenStateAsync(bool? isFs)
        {
            IsFullscreen = isFs ?? await IsFullscreenActiveSafeAsync();

            // сбрасываем старое значение, чтобы не использовать его по ошибке
            _viewerInnerHeight = 0;

            // даём браузеру дорисовать полноэкранный layout
            await Task.Delay(80);

            // меряем уже в новом размере
            await EnsureViewerSizeAsync();

            await FocusViewer();
            await InvokeAsync(StateHasChanged);
        }



        // === Fullscreen callbacks (как в вашем коде) ===
        [JSInvokable("OnFullscreenChanged")]
        public Task OnFullscreenChangedJs() => UpdateFullscreenStateAsync(null);

        // вызов из JS С параметром (если в js сразу вычисляешь isFs)
        [JSInvokable("OnFullscreenChangedBool")]
        public Task OnFullscreenChangedBoolJs(bool isFs) => UpdateFullscreenStateAsync(isFs);
        // Space → ваш старт/мягкая пауза (та же кнопка ▶️)
        [JSInvokable("OnTimedEventAsync")]
        public async Task OnTimedEventAsyncJs()
        {
            await OnTimedEventAsync(); // вызываем существующий метод без дублирования логики
        }

        // P → ваш явный PauseAsync (та же кнопка ⏸)
        [JSInvokable("PauseAsync")]
        public Task PauseAsyncJs() => PauseAsync();

        // ↓/↑ — шаги (опционально, если используете)
        [JSInvokable("StepNext")]
        public Task StepNextJs() => StepNext();

        [JSInvokable("StepPrev")]
        public Task StepPrevJs() => StepPrev();
        // безопасная проверка fullscreen без падений, если viewer.* нет
        private async Task<bool> IsFullscreenActiveSafeAsync()
        {
            try
            {
                return await JS.InvokeAsync<bool>("viewer.isFullscreenActive");



            }
            catch
            {
                try
                {
                    return await JS.InvokeAsync<bool>("eval",
                        "!!(document.fullscreenElement||document.webkitFullscreenElement||document.msFullscreenElement||document.mozFullScreenElement)");
                }
                catch { return false; }
            }
        }

        // ================= ИЗМЕРИТЕЛЬНАЯ ЛИНЕЙКА =================

        private ElementReference _viewerInnerRef;

        // состояние измерения
        private bool _isMeasuring;          // сейчас тянем линию (после первого клика)
        private bool _hasMeasurement;       // линия зафиксирована вторым кликом

        private double _startX;
        private double _startY;
        private double _currentX;
        private double _currentY;

        private double _viewerInnerHeight;  // фактическая высота картинки в пикселях
        private string _measurementLabel;   // текст вида "1 см 2 мм"

        // DTO для JS (C# 9 → обычный класс, не record struct)
        private class DomRect
        {
            public double Width { get; set; }
            public double Height { get; set; }
        }

        private async Task EnsureViewerSizeAsync()
        {
            try
            {
                var rect = await JS.InvokeAsync<DomRect>(
                    "blazorMeasure_getElementRect", _viewerInnerRef);

                _viewerInnerHeight = rect?.Height ?? 0;
            }
            catch
            {
                _viewerInnerHeight = 0;
            }
        }




        private async Task OnImageMouseDown(MouseEventArgs e)
        {
            // только левая кнопка
            if (e.Button != 0)
                return;

            await EnsureViewerSizeAsync();

            var x = e.OffsetX;
            var y = e.OffsetY;

            // первый клик — начало
            if (!_isMeasuring && !_hasMeasurement)
            {
                _startX = x;
                _startY = y;
                _currentX = x;
                _currentY = y;
                _isMeasuring = true;
                _hasMeasurement = false;
                UpdateMeasurement();
            }
            // второй клик — конец
            else if (_isMeasuring)
            {
                _currentX = x;
                _currentY = y;
                _isMeasuring = false;
                _hasMeasurement = true;
                UpdateMeasurement();
            }
            // третий клик — начать заново
            else
            {
                _startX = x;
                _startY = y;
                _currentX = x;
                _currentY = y;
                _isMeasuring = true;
                _hasMeasurement = false;
                UpdateMeasurement();
            }
        }

        private void OnImageMouseMove(MouseEventArgs e)
        {
            if (!_isMeasuring)
                return;

            _currentX = e.OffsetX;
            _currentY = e.OffsetY;
            UpdateMeasurement();
        }

        private void UpdateMeasurement()
        {
            if ((_isMeasuring || _hasMeasurement) &&
                _viewerInnerHeight > 0 &&
                N_rows > 0)
            {
                // Полная длина линии в пикселях (можно и вертикаль, и диагональ)
                var dx = _currentX - _startX;
                var dy = _currentY - _startY;
                var distancePx = Math.Sqrt(dx * dx + dy * dy);

                // Сколько пикселей приходится на один кадр по вертикали
                var pixelsPerFrame = _viewerInnerHeight / (double)N_rows;   // px / кадр

                // Пикселей на метр (5 кадров = 1 метр)
                var pixelsPerMeter = pixelsPerFrame * FramesPerMeter;       // px / м
                if (pixelsPerMeter <= 0)
                {
                    _measurementLabel = string.Empty;
                }
                else
                {
                    var metersPerPixel = 1.0 / pixelsPerMeter;              // м / px
                    var distanceMeters = distancePx * metersPerPixel;
                    var distanceMm = distanceMeters * 1000.0;

                    _measurementLabel = FormatMm(distanceMm);
                }
            }
            else
            {
                _measurementLabel = string.Empty;
            }

            _ = InvokeAsync(StateHasChanged);
        }





        private static string FormatMm(double mm)
        {
            mm = Math.Abs(mm);
            var totalMm = (int)Math.Round(mm);

            var cm = totalMm / 10;
            var restMm = totalMm % 10;

            if (cm == 0)
                return $"{totalMm} мм";

            if (restMm == 0)
                return $"{cm} см";

            return $"{cm} см {restMm} мм";
        }

        // =========================================================
    }
}
