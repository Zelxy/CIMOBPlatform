﻿using CIMOBProject.Models;
using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace BackOfficeWPF.Dialogs {
    /// <summary>
    /// Interaction logic for EmployeeDialog.xaml
    /// </summary>
    public partial class EmployeeDialog : Window
    {
        private readonly ApplicationDbContext _db = new ApplicationDbContext();
        private bool mayClose;
        private bool registerMode;
        public Employee Employee { get; set; }
        public string Password { get; set; }

        public EmployeeDialog(Employee employee = null)
        {
            InitializeComponent();
            if(employee == null)
            {
                registerMode = true;
            }
            this.Employee = employee ?? new Employee();
            Employee.BirthDate = new DateTime(2000, 1, 1);
            GridFormEmployee.DataContext = Employee;
            mayClose = false;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            mayClose = true;
            if (!UpdateEmployee())
            {
                mayClose = false;
            }
            else
            {
                Password = employeePassword.Password;
            }

            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            mayClose = true;
        }

        private bool UpdateEmployee()
        {
            if (Employee.PhoneNumber == null || Employee.PhoneNumber.Length > 12)
            {
                MessageBox.Show("O número de telemóvel é inválido.", "Erro - Telemóvel inválido!");
                return false;
            }
            if (Employee.EmployeeNumber == null || Employee.EmployeeNumber.Length > 9)
            {
                MessageBox.Show("O número de empregado é inválido.", "Erro - Número de empregado inválido!");
                return false;
            }
            if (Employee.PostalCode == null || Employee.PostalCode.Equals(""))
            {
                MessageBox.Show("O código postal é inválido.", "Erro - Código postal inválido!");
                return false;
            }
            if (Employee.UserFullname == null || Employee.UserFullname.Equals(""))
            {
                MessageBox.Show("O nome é obrigatório.", "Erro - Nome obrigatório!");
                return false;
            }
            if (Employee.UserName == null || Employee.UserName.Equals(""))
            {
                MessageBox.Show("O email é obrigatório.", "Erro - Email obrigatório!");
                return false;
            }
            if (Employee.UserCc == null || Employee.UserCc.Length > 8 || Employee.UserCc.Equals(""))
            {
                MessageBox.Show("O cc é obrigatório.", "Erro - CC obrigatório!");
                return false;
            }
            if (Employee.UserAddress == null || Employee.UserAddress.Equals(""))
            {
                MessageBox.Show("A morada é obrigatória.", "Erro - Morada obrigatória!");
                return false;
            }
            
            return true;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = !mayClose;
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }


        private void PostalCode(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("^[0-9]{4}[ -]?[0-9]{3}");
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}
