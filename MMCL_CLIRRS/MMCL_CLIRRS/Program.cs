namespace MMCL_CLIRRS
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            IReservationRepository repository = new MockReservationRepository();
            var service = new ReservationSearchService(repository);

            Application.Run(new ReservationSearchForm(service));
        }
    }
}