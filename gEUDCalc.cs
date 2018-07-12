using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace VMS.TPS
{
  public class Script
  {
    public Script() { }

    public void Execute(ScriptContext context /*, System.Windows.Window window*/ )
    {
      plan = context.ExternalPlanSetup;
      var structures = context.StructureSet.Structures;
      structureCombo.ItemsSource =
        structures.Where(s => s.DicomType != "SUPPORT" && !s.IsEmpty).OrderBy(s => s.Id).Select(s => s.Id);
      ShowCalcWindow();
    }

    ExternalPlanSetup plan;
    ComboBox structureCombo = new ComboBox();
    Window MainWindow = new Window();
    TextBox inputBox = new TextBox();
    TextBox outputBox = new TextBox();
    bool isInputBoxOK;

    private void ShowCalcWindow()
    {
      isInputBoxOK = false;

      var mainBorder = new Border();
      mainBorder.Padding = new Thickness(15);

      var mainPanel = new StackPanel();
      mainPanel.Orientation = Orientation.Vertical;

      structureCombo.Width = 200;
      structureCombo.HorizontalAlignment = HorizontalAlignment.Center;
      structureCombo.Margin = new Thickness(10);

      // Input and Output region
      var ioPanel = new StackPanel();
      ioPanel.Orientation = Orientation.Horizontal;

      var inputPanel = new StackPanel();
      inputPanel.Orientation = Orientation.Vertical;

      var inputLabel = new Label();
      inputLabel.Content = "a";
      inputLabel.HorizontalAlignment = HorizontalAlignment.Center;
      inputLabel.Margin = new Thickness(10, 10, 10, 5);

      inputBox.HorizontalAlignment = HorizontalAlignment.Center;
      inputBox.Width = 80;
      inputBox.TextAlignment = TextAlignment.Center;
      inputBox.Margin = new Thickness(10, 5, 10, 10);
      inputBox.TextChanged += new TextChangedEventHandler(inputBox_changed);

      inputPanel.Children.Add(inputLabel);
      inputPanel.Children.Add(inputBox);

      var outputPanel = new StackPanel();
      outputPanel.Orientation = Orientation.Vertical;

      var outputLabel = new Label();
      outputLabel.Content = "gEUD (Gy)";
      outputLabel.HorizontalAlignment = HorizontalAlignment.Center;
      outputLabel.Margin = new Thickness(10, 10, 10, 5);

      outputBox.Width = 100;
      outputBox.TextAlignment = TextAlignment.Center;
      outputBox.Margin = new Thickness(10, 5, 10, 10);
      outputBox.IsReadOnly = true;
      outputBox.FontWeight = FontWeights.Bold;
      outputBox.Background = Brushes.Snow;

      outputPanel.Children.Add(outputLabel);
      outputPanel.Children.Add(outputBox);

      ioPanel.Children.Add(inputPanel);
      ioPanel.Children.Add(outputPanel);

      // calculate button
      var calButton = new Button();
      calButton.Content = "Calculate";
      calButton.HorizontalAlignment = HorizontalAlignment.Right;
      calButton.Padding = new Thickness(10, 5, 10, 5);
      calButton.Margin = new Thickness(10, 10, 10, 30);

      calButton.Click += new RoutedEventHandler(calButton_click);

      mainPanel.Children.Add(structureCombo);
      mainPanel.Children.Add(ioPanel);
      mainPanel.Children.Add(calButton);

      mainBorder.Child = mainPanel;

      MainWindow.Title = "gEUD Calculator";
      MainWindow.Content = mainBorder;
      MainWindow.FontSize = 16;
      MainWindow.SizeToContent = SizeToContent.WidthAndHeight;
      MainWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
      MainWindow.ShowDialog();

    }

    private void calButton_click(object sender, RoutedEventArgs e)
    {
      if (isInputBoxOK && structureCombo.SelectedItem != null)
      {
        var input = Convert.ToDouble(inputBox.Text);
        var selectedStructure = plan.StructureSet.Structures.First(s => s.Id == structureCombo.SelectedItem.ToString());

        double binwidth = 0.1;

        var dvh = plan.GetDVHCumulativeData(selectedStructure, DoseValuePresentation.Relative, VolumePresentation.Relative, binwidth);

        double gEUD = 0;

        for (int i = 0; i < dvh.CurveData.Length - 1; ++i)
        {
          gEUD += (dvh.CurveData[i].Volume - dvh.CurveData[i + 1].Volume) * Math.Pow((i * binwidth + binwidth / 2), input) / 100;
        }

        gEUD = Math.Pow(gEUD, 1 / input);

        var preDose = plan.TotalPrescribedDose;
        var doseUnit = plan.TotalPrescribedDose.UnitAsString;

        var preDoseInGy = doseUnit == "Gy" ? preDose.Dose : preDose.Dose / 100;

        gEUD = gEUD * preDoseInGy / 100;

        outputBox.Text = gEUD.ToString("F2");

      }
    }

    private void inputBox_changed(object sender, TextChangedEventArgs e)
    {
      isInputBoxOK = false;

      if (inputBox.Text == "") return;

      if (inputBox.Text != "-") 
      {
        double ab = 1;
        var format = new System.Globalization.NumberFormatInfo();
        format.NegativeSign = "-";
        format.NumberDecimalSeparator = ".";

        try
        {
          ab = Double.Parse(inputBox.Text, format);
        }
        catch (FormatException)
        {
          MessageBox.Show("適切な値を入力してください。");
          inputBox.Clear();
        }

        isInputBoxOK = true;
      }
    }
  }
}