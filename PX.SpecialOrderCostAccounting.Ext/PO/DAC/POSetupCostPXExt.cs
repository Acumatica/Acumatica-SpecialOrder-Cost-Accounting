using System;
using PX.Data;
using PX.Objects.IN;
using PX.Objects.PO;

namespace PX.SpecialOrderCostAccounting.Ext
{
    public sealed class POSetupCostPXExt : PXCacheExtension<POSetup>
    {
        #region UsrFreightInventory
        public abstract class usrFreightInventory : Data.BQL.BqlInt.Field<usrFreightInventory> { }

        [NonStockItem(DisplayName = "Freight Item", Filterable = true)]
        public int? UsrFreightInventory { get; set; }
        #endregion
    }
}