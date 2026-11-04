namespace MMCL_CLIRRS
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            // 1. The linked list is now the master storage of all reservations.
            //    It starts with the sample data so the demo is not empty.
            var reservations = new ReservationLinkedList();
            foreach (Reservation r in new MockReservationRepository().GetAll())
                reservations.Add(r);

            // 2. Functionality #4 (search) builds its indexes from that list.
            var searchService = new ReservationSearchService(reservations);

            // 3. Functionality #1 (reservation management) uses both of them.
            var managementService = new ReservationManagementService(reservations, searchService);

            // 4. Functionality #2 (scheduling): watches the 5-minute grace period and
            //    releases seats nobody came for. A timer inside MainMenuForm drives it.
            var monitorService = new ReservationMonitorService(reservations, searchService);

            Application.Run(new MainMenuForm(managementService, searchService, monitorService));
        }
    }
}
