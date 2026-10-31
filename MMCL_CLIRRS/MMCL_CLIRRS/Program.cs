namespace MMCL_CLIRRS
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            var repo = new MockReservationRepository();
            var service = new ReservationSearchService(repo);

            MessageBox.Show(
                $"ID 1003: {service.FindById(1003)?.UserName}\n" +
                $"Maria Santos: {service.SearchByName("maria santos").Count}\n" +
                $"Cancelled: {service.SearchByStatus(ReservationStatus.Cancelled).Count}\n" +
                $"Discussion rooms: {service.GetDiscussionRooms().Count}");
            ApplicationConfiguration.Initialize();
            Application.Run(new ReservationSearchForm());
        }
    }
}