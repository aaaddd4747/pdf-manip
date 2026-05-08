using System;
using System.Linq;
using System.Threading.Tasks;
using PdfSharp.Pdf;
using Avalonia.LogicalTree;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using PdfSharp.Pdf.IO;
using System.Collections.Generic;

namespace pdf_manip;

public partial class MainWindow : Window {
	private TextBlock file1_readout;
	private TextBlock file2_readout;
	private TextBlock status;
	private ProgressBar prog;
	public MainWindow() {
		InitializeComponent();
		var blocks = this.GetLogicalDescendants().OfType<TextBlock>().ToList();
		foreach (TextBlock block in blocks) {
			switch (block.Name) {
				case "txt1":
					file1_readout = block;
					break;
				case "txt2":
					file2_readout = block;
					break;
				case "txt3":
					status = block;
					break;
				default:
					break;
			}
		}
		prog = this.GetLogicalDescendants().OfType<ProgressBar>().First();
	}

	async Task<IStorageFile?> open_file_dialogue(string title) {
		var topLevel = TopLevel.GetTopLevel(this);
		var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions {
			Title = title,
			AllowMultiple = false,
			FileTypeFilter = [FilePickerFileTypes.Pdf]
		});
		if (files.Count() >= 1) {
			return files.First();
		}
		return null;
	}

	async Task<IStorageFile?> new_file_dialogue(string title) {
		var topLevel = TopLevel.GetTopLevel(this);
		var files = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions {
			Title = title,
			DefaultExtension = "pdf",
			FileTypeChoices = [FilePickerFileTypes.Pdf]
		});
		return files;
	}


	public async void Button_on_click(object? sender, RoutedEventArgs e) {
		Button? s = (Button?)sender;
		if (s is null) {
			return;
		}
		Console.WriteLine($"clicked {s?.Name} : {e}");
		string name = s?.Name ?? "";
		var textboxes =
			from box in this.GetLogicalDescendants().OfType<TextBlock>().ToList()
			where box.Name != ""
			select box;
		if (name.Contains("src")) {
			IStorageFile? file = await open_file_dialogue($"Select source {name[3..]}");
			if (file is not null) {
				Console.WriteLine($"selected: {unfuck(file.Path.AbsolutePath)}");
				var tbox = from box in textboxes
						   where box.Name?.Contains($"txt{name[3..]}") ?? false
						   select box;
				tbox.First().Text = unfuck(file.Path.AbsolutePath);
			}
		}
	}
	private string unfuck(string x) {
		//i could probably refactor this function out but it's funny to have a function called unfuck that actually does what it says, so i won't.
		return Uri.UnescapeDataString(x);
	}

	public async void Run_job(object? sender, RoutedEventArgs e) {
		var f3 = await new_file_dialogue("select output file");
		if (f3 is null) {
			return;
		}
		Console.WriteLine($"{unfuck(file1_readout.Text)}\n{unfuck(file2_readout.Text)}");
		var p1 = PdfReader.Open(unfuck(file1_readout.Text), PdfDocumentOpenMode.Import);
		var p2 = PdfReader.Open(unfuck(file2_readout.Text), PdfDocumentOpenMode.Import);

		PdfDocument p3 = new();
		List<PdfPage> pages1 = [];
		List<PdfPage> pages2 = [];

		foreach (var page in p1.Pages) {
			pages1.Add(page);
		}
		foreach (var page in p2.Pages) {
			pages2.Add(page);
		}
		pages2.Reverse();
		var pages3 = pages1.Zip<PdfPage, PdfPage>(pages2);
		var progress = 0;
		prog.Maximum = pages3.Count();
		foreach (var page in pages3) {
			p3.AddPage(page.First);
			progress++;
			status.Text = $"processing page: {progress}";
			prog.Value = progress;
			p3.AddPage(page.Second);
			progress++;
			status.Text = $"processing page: {progress}";
			prog.Value = progress;
		}
		await p3.SaveAsync(unfuck(f3.Path.AbsolutePath));
		status.Text = "[idle]";
	}
}