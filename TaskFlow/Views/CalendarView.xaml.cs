using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TaskFlow.Models;

namespace TaskFlow.Views
{
    public partial class CalendarView : UserControl
    {
        private List<TaskItem> _allTasks = new List<TaskItem>();

        public CalendarView()
        {
            InitializeComponent();
            MainCalendar.SelectedDate = DateTime.Today;
        }

        // Метод для передачи всех задач из MainWindow
        public void SetTasks(List<TaskItem> tasks)
        {
            _allTasks = tasks ?? new List<TaskItem>();
            UpdateTasksForSelectedDate();
        }

        private void MainCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateTasksForSelectedDate();
        }

        private void UpdateTasksForSelectedDate()
        {
            if (MainCalendar.SelectedDate == null)
                return;

            DateTime selectedDate = MainCalendar.SelectedDate.Value.Date;

            txtSelectedDate.Text = $"Задачи на {selectedDate:dd MMMM yyyy}";

            var tasksForDay = _allTasks
                .Where(t => t.DueDate.HasValue && t.DueDate.Value.Date == selectedDate)
                .OrderBy(t => t.Status)
                .ToList();

            lvDayTasks.ItemsSource = tasksForDay;

            txtNoTasks.Visibility = tasksForDay.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }
}