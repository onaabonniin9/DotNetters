using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using GenteFit.Data;
using Microsoft.EntityFrameworkCore;
using GenteFit.Models;
using GenteFit.Services;


namespace GenteFit.WPF
{
    /// <summary>
    /// Lógica de interacción para ClientesXmlWindow.xaml
    /// </summary>
    public partial class ClientesXmlWindow : Window
    {
        public ClientesXmlWindow()
        {
            InitializeComponent();
        }

        private async void BtnExportar_Click(object sender, RoutedEventArgs e)
        {
            var selector = new SaveFileDialog
            {
                Title = "Guardar clientes como XML",
                FileName = "clientes_exportados.xml",
                Filter = "Archivos XML (*.xml)|*.xml",
                DefaultExt = ".xml",
                AddExtension = true
            };

            if (selector.ShowDialog(this) != true)
                return;

            BtnExportar.IsEnabled = false;

            try
            {
                var factory = new GenteFitContextFactory();
                await using var contexto = factory.CreateDbContext(Array.Empty<string>());

                var servicio = new ClienteService(contexto);
                var clientes = await servicio.ConsultarTodasAsync();

                var elementosClientes = clientes.Select(cliente =>
                    new XElement("Cliente",
                        new XElement("Id", cliente.Id),
                        new XElement("Name", cliente.Name),
                        new XElement("Surname", cliente.Surname),
                        new XElement("Username", cliente.Username),
                        new XElement("Password", cliente.Password),
                        new XElement("Email", cliente.Email),
                        new XElement("Phone", cliente.Phone),
                        new XElement("Active", cliente.Active),
                        new XElement("DateCreated", cliente.DateCreated),
                        new XElement("DNI", cliente.DNI),
                        new XElement("BirthDate", cliente.BirthDate),
                        new XElement("Address", cliente.Address),
                        new XElement("City", cliente.City),
                        new XElement("PostalCode", cliente.PostalCode),
                        new XElement("IBAN", cliente.IBAN)
                    ));

                var documento = new XDocument(
                    new XElement("GenteFit",
                        new XElement("Clientes", elementosClientes)));

                documento.Save(selector.FileName);

                MessageBox.Show(this,
                    $"Clientes exportados: {clientes.Count}\n\n" +
                    $"Archivo guardado en:\n{selector.FileName}",
                    "Exportación completada");
            }
            catch (Microsoft.Data.SqlClient.SqlException)
            {
                MessageBox.Show(this, "No se han podido consultar los clientes en SQL Server.");
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show(this, "No tienes permiso para guardar en esa carpeta.");
            }
            catch (IOException)
            {
                MessageBox.Show(this, "No se ha podido escribir el archivo. Comprueba si está en uso.");
            }
            catch (ArgumentException)
            {
                MessageBox.Show(this, "Comprueba la ruta y que los datos contengan caracteres válidos para XML.");
            }
            finally
            {
                BtnExportar.IsEnabled = true;
            }
        }
        private async void BtnImportar_Click(object sender, RoutedEventArgs e)
        {
            var selector = new OpenFileDialog
            {
                Title = "Seleccionar archivo de clientes",
                Filter = "Archivos XML (*.xml)|*.xml"
            };

            if (selector.ShowDialog(this) == true)
            {
                try
                {
                    var documento = XDocument.Load(selector.FileName);

                    var clientesXml = documento.Descendants("Cliente").ToList();

                    

                    var factory = new GenteFitContextFactory();
                    await using var contexto = factory.CreateDbContext(Array.Empty<string>());

                    var dniGuardados = await contexto.Clientes
                        .Select(cliente => cliente.DNI)
                        .ToListAsync();

                    var dniExistentes = dniGuardados
                        .Where(dni => !string.IsNullOrWhiteSpace(dni))
                        .Select(dni => dni.Trim())
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                    int importados = 0;
                    int omitidos = 0;
                    int errores = 0;

                    foreach (var elemento in clientesXml)
                    {
                        string dni = ((string?)elemento.Element("DNI") ?? "").Trim();

                        if (dni.Length == 0 || dniExistentes.Contains(dni))
                        {
                            omitidos++;
                            continue;
                        }

                        try
                        {
                            var cliente = new Cliente
                            {
                                Name = (string?)elemento.Element("Name") ?? "",
                                Surname = (string?)elemento.Element("Surname") ?? "",
                                Username = (string?)elemento.Element("Username") ?? "",
                                Password = (string?)elemento.Element("Password") ?? "",
                                Email = (string?)elemento.Element("Email") ?? "",
                                Phone = (string?)elemento.Element("Phone") ?? "",
                                Active = (bool?)elemento.Element("Active") ?? true,
                                DateCreated = (DateTime?)elemento.Element("DateCreated") ?? DateTime.UtcNow,
                                DNI = dni,
                                BirthDate = (DateTime?)elemento.Element("BirthDate") ?? DateTime.MinValue,
                                Address = (string?)elemento.Element("Address") ?? "",
                                City = (string?)elemento.Element("City") ?? "",
                                PostalCode = (string?)elemento.Element("PostalCode") ?? "",
                                IBAN = (string?)elemento.Element("IBAN") ?? ""
                            };

                            await using var contextoAlta = factory.CreateDbContext(Array.Empty<string>());
                            var servicio = new ClienteService(contextoAlta);
                            await servicio.CrearAsync(cliente);

                            dniExistentes.Add(dni);
                            importados++;
                        }
                        catch (Exception ex) when (ex is ArgumentException
                                                      or FormatException
                                                      or DbUpdateException)
                        {
                            errores++;
                        }
                    }

                    MessageBox.Show(this,
                        $"Clientes leídos: {clientesXml.Count}\n" +
                        $"Importados correctamente: {importados}\n" +
                        $"Omitidos por DNI repetido o vacío: {omitidos}\n" +
                        $"Errores: {errores}",
                        "Resultado de la importación");
                }
                catch (XmlException)
                {
                    MessageBox.Show(this, "El archivo tiene un formato XML incorrecto.");
                }
                catch (IOException)
                {
                    MessageBox.Show(this, "No se ha podido leer el archivo seleccionado.");
                }
                catch (UnauthorizedAccessException)
                {
                    MessageBox.Show(this, "No tienes permiso para leer este archivo.");
                }
                catch (Microsoft.Data.SqlClient.SqlException)
                {
                    MessageBox.Show(this, "No se han podido consultar los clientes en SQL Server.");
                }
            }
        }
    }
}
