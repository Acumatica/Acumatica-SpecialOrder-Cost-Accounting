[![Project Status](http://opensource.box.com/badges/active.svg)](http://opensource.box.com/badges)

Extension that enables tracking and using of actual Inventory Costs that are specific to a particular job/order
==================================

This extension enables the system to use a specific Purchase Cost for Special Order items that flows through to Inventory and Financial documents as a Cost of Goods Sold, providing a more precise profitability calculation for the Sales/Service Order.

A Special Order item is considered any item that has been acquired for a specific job only, for a special purchase cost from a Vendor. These items are usually not kept in Inventory due to their nature, specific components, dimensions, attributes, etc. and are specifically ordered for a specific Service/Sales Order. In certain scenarios (returns, volume based discounts, etc), a Merchant may have these Special Order items in Inventory, but they are still flagged as Special Order items and comply with this customization.

Functionality included in this extension:

* Define Stock Item as Special Order
* Allows user to specify cost for Special Order type line item in Sales/Service order and that flows through inventory and financial docuemnts.
* Warns user upon changing Quantity and Unit Cost in Purchase Order linked with Sales/Service order and updates in linked Sales/Service order if user selects to do so.
* Validates deleting purchase order line or entire order if linked with Sales/Service order
* Reference link to open Sales/Service order is added for linked purchase order.

### Prerequisites
* Acumatica 2020 R1 (20.103.0019 or higher) [2020 R1 Source and Deployment Package](https://github.com/Acumatica/Acumatica-SpecialOrder-Cost-Accounting/tree/2020R1)
* Acumatica 2020 R2 (20.213.0026 or higher) [2020 R2 Source and Deployment Package](https://github.com/Acumatica/Acumatica-SpecialOrder-Cost-Accounting/tree/2020R2)
* Acumatica 2021 R2 (21.217.0035 or higher) [2021 R2 Source and Deployment Package](https://github.com/Acumatica/Acumatica-SpecialOrder-Cost-Accounting/tree/2021R2)
* Acumatica 2023 R2 (23.212.0024 or higher) [2023 R2 Source and Deployment Package](https://github.com/Acumatica/Acumatica-SpecialOrder-Cost-Accounting/tree/2023R2)
  
Quick Start
-----------

### Installation

##### Install customization deployment package
1. Download PXSpecialOrderCostPkg.zip.
2. In your Acumatica ERP instance, navigate to System -> Customization -> Customization Projects (SM204505), import PXSpecialOrderCostPkg.zip as a customization project
3. Publish customization project.

### Usage

A Special Order Only checkbox has been added to the Stock Items screen (INS202500) to flag an Item as a Special Order item. Only Items with the following settings can be flagged as a Special Order item:

   * Item is not lot/serialized tracked
   * Valuation Method is set to Average

![Screenshot](/_ReadMeImages/IN202500a.png)

When an Item flagged as a Special Order item is used on a Service/Sales Order:

   * Mark for PO checkbox is automatically selected for the item
   * Unit Cost field for the item becomes editable
   * Any Line Note added to a Special Order item will get copied to the related IN Issue document created from processing the Service/Sales Order 

The Unit Cost added on the Service/Sales Order flows through to the Purchase Order created for the Service/ Sales Order. As the Purchase Order is linked to the Service/Sales Order any changes done to the Quantity or Unit Cost for Special Order items or regular Items on the Purchase Order will trigger a change on the related Service/Sales Order. The System will show a Dialog Message window for the User to choose if changes should be done on the Purchase Order and related Service/Sales Order or no changes should be done at all.

![Screenshot](/_ReadMeImages/PO301000a.png)

Validation is added on deleting a purchase order line or entire order for Purchase orders that are linked to a Service/Sales Order.

![Screenshot](/_ReadMeImages/PO301000b.png)

The Purchase Order contains links to the related Service/Sales Order it was created for. These links are displayed in a line column called "Sales/Service Order #" on the Purchase Order -> Document Details tab. 

![Screenshot](/_ReadMeImages/PO301000c.png)

If a Purchase Order is created for multiple Service/Sales Orders, the link displays Multiple, which invokes a separate window, displaying the Service/Sales Order the Purchase Order was created for. If a Purchase Order was created for multiple Service/Sales Orders, changing Quantities or Unit Cost on the Purchase Order is not allowed.

![Screenshot](/_ReadMeImages/PO301000d.png)

Once a Purchase Receipt is processed for the Purchase Order, the system will update the Average Cost for the Special Order item on the Stock Items screen. No changing of Quantities is allowed on the Purchase Receipt.

![Screenshot](/_ReadMeImages/PO302000.png)

When the IN Issue document is created for the Service/Sales order, either through Invoicing the job/order or using Update IN, on release, the Average cost of the Item gets recalculated to exclude the Purchase costs for the Special Order item.

![Screenshot](/_ReadMeImages/IN202500b.png)

The created IN Issue document will have:
   * The same Unit Cost as used on the related Service/Sales Order
   * Any Line Note for a Special Order item will show as a Line Note for the Item on the IN Issue

![Screenshot](/_ReadMeImages/SO301000.png)

When the IN document is released, the created GL Batch is using the exact same Unit Cost that was specified on the related Service/Sales Order. Inventory Asset Account and COGS Account get updated with the actual purchase cost for that specific Service/Sales Order. 

![Screenshot](/_ReadMeImages/Image10.png)

Known Issues
------------
None at the moment

## Copyright and License

Copyright © `2020` `Acumatica, INC`

This component is licensed under the MIT License, a copy of which is available online [here](LICENSE)
