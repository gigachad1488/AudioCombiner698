using AudioCombiner698.ViewModels;
using Avalonia.Controls;
using Avalonia.Threading;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace AudioCombiner698.Views;

public partial class MainWindow : Window
{
    public string importFolderPath;
    public string exportFolderPath;

    public MainWindow()
    {
        InitializeComponent();

        resultText.Text = string.Empty;
        importPathText.Text = string.Empty;
        exportPathText.Text = string.Empty;

        importButton.Click += delegate { SetImport(); };
        exportButton.Click += delegate { SetExport(); };
        combineButton.Click += delegate { Combine(); };
    }

    public async void SetImport()
    {
        var folder = await StorageProvider.OpenFolderPickerAsync(new Avalonia.Platform.Storage.FolderPickerOpenOptions
        { AllowMultiple = false });

        if (folder.Count > 0)
        {
            importFolderPath = folder[0].Path.AbsolutePath;
            importPathText.Text = importFolderPath;
        }
    }

    public async void SetExport()
    {
        var folder = await StorageProvider.OpenFolderPickerAsync(new Avalonia.Platform.Storage.FolderPickerOpenOptions
        { AllowMultiple = false });

        if (folder.Count > 0)
        {
            exportFolderPath = folder[0].Path.AbsolutePath;
            exportPathText.Text = exportFolderPath;
        }
    }

    public async void Combine()
    {
        resultText.Text = "Обработка...";
        combineButton.IsEnabled = false;

        await Task.Run(() =>
        {
            try
            {
                if (string.IsNullOrEmpty(importFolderPath))
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        resultText.Text = "Папка импорта не указана";
                    });

                    return;
                }
                if (string.IsNullOrEmpty(exportFolderPath))
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        resultText.Text = "Папка экспорта не указана";
                    });

                    return;
                }
                string[] paths = [];
                List<ISampleProvider> readers = new List<ISampleProvider>();
                List<AudioFileReader> fileriders = new List<AudioFileReader>();

                switch (formatsCombobox.SelectedIndex)
                {
                    case 0:
                        paths = Directory.EnumerateFiles(importFolderPath, "*.*").Where(x => x.EndsWith(".mp3") || x.EndsWith(".wav")).ToArray();
                        break;
                    case 1:
                        paths = Directory.GetFiles(importFolderPath, "*.mp3");
                        break;
                    case 2:
                        paths = Directory.GetFiles(importFolderPath, "*.wav");
                        break;
                }              

                if (paths.Length <= 0)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        resultText.Text = "В папке нет мп3";
                    });

                }

                foreach (string path in paths)
                {
                    var r = new AudioFileReader(path);
                    fileriders.Add(r);
                }

                int sampleRate = 8000;

                switch (samplesCombobox.SelectedIndex)
                {
                    case 0:
                        sampleRate = fileriders.Min(x => x.WaveFormat.SampleRate);
                        break;
                    case 1:
                        sampleRate = Convert.ToInt32(fileriders.Average(x => x.WaveFormat.SampleRate));
                        break;
                    case 2:
                        sampleRate = fileriders.Max(x => x.WaveFormat.SampleRate);
                        break;
                    case 3:
                        sampleRate = 8000;
                        break;
                    case 4:
                        sampleRate = 22050;
                        break;
                    case 5:
                        sampleRate = 44100;
                        break;
                    case 6:
                        sampleRate = 48000;
                        break;
                }

                int channels = channelsCombobox.SelectedIndex + 1;

                Dispatcher.UIThread.Post(() =>
                {
                    resultText.Text = $"Обработка...\r\n{fileriders.Count} файлов\r\nЧастота дискретизации: " + sampleRate.ToString() + "\r\nКаналов: " + channels;
                });

                var outFormat = new WaveFormat(sampleRate, channels);

                foreach (var r in fileriders)
                {
                    ISampleProvider provider = null;

                    if (r.WaveFormat.SampleRate < sampleRate || r.WaveFormat.SampleRate > sampleRate || r.WaveFormat.Channels != channels)
                    {
                        provider = new MediaFoundationResampler(r.ToWaveProvider(), outFormat).ToSampleProvider();
                    }
                    else
                    {
                        provider = r.ToSampleProvider();
                    }

                    readers.Add(provider);
                }

                ISampleProvider combined = new ConcatenatingSampleProvider(readers);

                string name = Guid.NewGuid().ToString() + ".wav";

                WaveFileWriter.CreateWaveFile16(exportFolderPath + "/" + name, combined);

                //MediaFoundationEncoder.EncodeToMp3(combined.ToWaveProvider(), Guid.NewGuid().ToString(), minSampleRate);

                Dispatcher.UIThread.Post(() =>
                {
                    resultText.Text = "Успешно создано под именем " + name + $"\r\nСоединено {fileriders.Count} файлов\r\nЧастота дискретизации: {sampleRate.ToString()}\r\nКаналов: {channels}";
                    combineButton.IsEnabled = true;
                });
            }
            catch
            {
                Dispatcher.UIThread.Post(() =>
                {
                    resultText.Text = "Ошибка обработки (невозможно)";
                    combineButton.IsEnabled = true;
                });
            }
        });
    }
}
