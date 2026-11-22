using System;
using System.Windows;
using System.Windows.Controls;

namespace GenteFit.WPF.Components;

public partial class BuscadorComun : UserControl
{
    public static readonly DependencyProperty IndicacionProperty =
        DependencyProperty.Register(nameof(Indicacion), typeof(string), typeof(BuscadorComun),
            new PropertyMetadata("Buscar..."));

    public string Indicacion
    {
        get => (string)GetValue(IndicacionProperty);
        set => SetValue(IndicacionProperty, value);
    }

    public string Text => CampoBusqueda?.Text ?? string.Empty;

    public event TextChangedEventHandler? TextChanged;

    public BuscadorComun() => InitializeComponent();

    private void CampoBusqueda_TextChanged(object sender, TextChangedEventArgs e)
        => TextChanged?.Invoke(this, e);
}
