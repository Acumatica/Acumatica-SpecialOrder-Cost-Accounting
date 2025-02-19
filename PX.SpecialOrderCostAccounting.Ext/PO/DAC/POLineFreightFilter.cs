using System;
using PX.Data;
using PX.Objects.IN;

namespace PX.SpecialOrderCostAccounting.Ext
{
    [Serializable]
    [PXHidden]
    public class AddFreightParams : PXBqlTable, IBqlTable
    {
        public abstract class freightAmount : PX.Data.BQL.BqlDecimal.Field<freightAmount> { }

        [PXDBPriceCost()]
        [PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
        [PXUIField(DisplayName = "Freight")]
        public virtual decimal? FreightAmount { get; set; }
    }
}