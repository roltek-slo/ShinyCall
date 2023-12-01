using ShinyCall.Mappings;
using ShinyCall.Sqlite;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ShinyCall.MVVM.View
{
    /// <summary>
    /// Interaction logic for LastProjectView.xaml
    /// </summary>
    public partial class LastProjectView : UserControl
    {
        public LastProjectView()
        {
            InitializeComponent();
            var task = Task.Run(async () => await RunForever());
            this.KeyDown += LastProjectView_KeyDown;

            list_box_links.MouseDoubleClick += List_box_links_MouseDoubleClick;

        }

        private void List_box_links_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (list_box_links.SelectedItems.Count > 0)
            {
                var selected = list_box_links.SelectedItems;
                foreach (string link in selected)
                {

                    LinksModel linksModel = new LinksModel();
                    linksModel.desc = link;
                    LinksModel lm = SqliteDataAccess.GetLinkBasedOnName(linksModel);
                    if(lm != null)
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = lm.link,
                            UseShellExecute = true
                        };
                        Process.Start(psi);
                    }
                }

            }
        }


        private void LastProjectView_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Delete)
            {
               if(list_box_contacts.SelectedItems.Count > 0)
                {
                    var selected = list_box_contacts.SelectedItems;
                    foreach (string contact in selected)
                    {
                   
                        var number_array =   contact.Split("-");
                        int number = int.Parse(number_array[number_array.Length - 1]);    
                        SqliteDataAccess.DeleteContact(number);
                    }
                } else
                {
                    if( list_box_links.SelectedItems.Count > 0)
                    {
                        var selected = list_box_links.SelectedItems;
                        foreach (string link in selected)
                        {
                            LinksModel lm = new LinksModel();
                            lm.desc = link;
                            SqliteDataAccess.DeleteLink(lm);
                        }
                    }
                }
            }
        }

        public async Task RunForever()
        {
            await Task.Run(() =>
            {
                while (true)
                {
                    UpdateUI();
                    Thread.Sleep(2000);
                }
            });
        }


        private System.Collections.IEnumerable GetListFromModelsList(List<ContactsModel> contacts)
        {
            foreach (var contact in contacts)
            {
                yield return $"{contact.name}-{contact.phone}";
            }
        }

        private System.Collections.IEnumerable GetListFromModelsLink(List<LinksModel> links)
        {
            foreach (var link in links)
            {
                yield return link.desc;
            }
        }

        private void UpdateUI()
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                var history = SqliteDataAccess.LoadDataContacts();
                var links = SqliteDataAccess.LoadDataLinks();            
                list_box_contacts.ItemsSource = GetListFromModelsList(history);
                list_box_links.ItemsSource = GetListFromModelsLink(links);              
            }));


        }
        private void DialogHost_DialogClosing(object sender, MaterialDesignThemes.Wpf.DialogClosingEventArgs eventArgs)
        {
      
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string name = name_contact.Text;
            string number = number_contact.Text;



            ContactsModel model = new ContactsModel();
            model.name = name;
            try
            {
                int numberParse = int.Parse(number_contact.Text);
                model.phone = numberParse;


                SqliteDataAccess.InsertContacts(model); 
            } catch (Exception err) {
                var se = err.Message;
                return; 
            }
            
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            LinksModel model = new LinksModel();
            model.link = link_obj.Text;
            model.desc = link_des.Text;

            try
            {


                SqliteDataAccess.InsertLinks(model);
            }
            catch (Exception) { return; }

        }
    }
}
