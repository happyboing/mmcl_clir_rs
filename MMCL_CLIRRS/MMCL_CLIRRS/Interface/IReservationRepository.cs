using System;
using System.Collections.Generic;
using System.Text;

namespace MMCL_CLIRRS
{
    public interface IReservationRepository
    {
        IEnumerable<Reservation> GetAll();
        Reservation? GetById(int reservationId);
        void Add(Reservation reservation);
    }
}
