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

                List<ISampleProvider> readers = new List<ISampleProvider>();
                List<AudioFileReader> fileriders = new List<AudioFileReader>();

                string[] paths = Directory.GetFiles(importFolderPath, "*.mp3");

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
                        sampleRate = fileriders.Max(x => x.WaveFormat.SampleRate);
                        break;
                    case 2:
                        sampleRate = 8000;
                        break;
                    case 3:
                        sampleRate = 22050;
                        break;
                    case 4:
                        sampleRate = 44100;
                        break;
                    case 5:
                        sampleRate = 48000;
                        break;
                }

                Dispatcher.UIThread.Post(() =>
                {
                    resultText.Text = "Обработка...\r\nЧастота дискретизации: " + sampleRate.ToString();
                });

                var outFormat = new WaveFormat(sampleRate, 1);

                foreach (var r in fileriders)
                {
                    ISampleProvider provider = null;

                    if (r.WaveFormat.SampleRate < sampleRate || r.WaveFormat.SampleRate > sampleRate || r.WaveFormat.Channels > 1)
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
                    resultText.Text = "Успешно создано под именем " + name;
                });
            }
            catch
            {
                Dispatcher.UIThread.Post(() =>
                {
                    resultText.Text = "Ошибка обработки (невозможно)";
                });
            }
        });
    }

    public static void Combine(string[] inputFiles, Stream output)
    {
        foreach (string file in inputFiles)
        {
            Mp3FileReader reader = new Mp3FileReader(file);
            if ((output.Position == 0) && (reader.Id3v2Tag != null))
            {
                output.Write(reader.Id3v2Tag.RawData, 0, reader.Id3v2Tag.RawData.Length);
            }
            Mp3Frame frame;
            while ((frame = reader.ReadNextFrame()) != null)
            {
                output.Write(frame.RawData, 0, frame.RawData.Length);
            }
        }
    }
}
