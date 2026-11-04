namespace MMCL_CLIRRS
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            var reservations = new ReservationLinkedList();
            foreach (Reservation r in new MockReservationRepository().GetAll())
                reservations.Add(r);

            var searchService = new ReservationSearchService(reservations);

            var managementService = new ReservationManagementService(reservations, searchService);

            var monitorService = new ReservationMonitorService(reservations, searchService);

            Application.Run(new MainMenuForm(managementService, searchService, monitorService));
        }
    }
}
