/*
 * Copyright © Tomasz Jastrzębski 2019
 */
using System;

/// <remarks>Note: attribute order must match database fields.</remarks>
public sealed class DetailedUsage
{
    public int AccountId { get; set; } // obsolete since V3
    public string AccountName { get; set; }
    public string AccountOwnerEmail { get; set; }
    public string AdditionalInfo { get; set; }
    public bool ChargesBilledSeparately { get; set; } // added in V3
    public decimal ConsumedQuantity { get; set; }
    public string ConsumedService { get; set; }
    public int ConsumedServiceId { get; set; } // obsolete since V3
    public decimal Cost { get; set; }
    public string CostCenter { get; set; }
    public DateTime Date { get; set; }
    public int DepartmentId { get; set; } // obsolete since V3
    public string DepartmentName { get; set; }
    public string InstanceId { get; set; }
    public string Location { get; set; } // added in V3
    public string MeterCategory { get; set; }
    public Guid MeterId { get; set; }
    public string MeterName { get; set; }
    public string MeterRegion { get; set; }
    public string MeterSubCategory { get; set; }
    public string OfferId { get; set; } // added in V3
    public string PartNumber { get; set; } // added in V3
    public string Product { get; set; }
    public int ProductId { get; set; } // obsolete since V3
    public string ResourceGroup { get; set; }
    public Guid? ResourceGuid { get; set; } // added in V3
    public string ResourceLocation { get; set; }
    public int ResourceLocationId { get; set; } // obsolete since V3
    public decimal ResourceRate { get; set; }
    public string ServiceAdministratorId { get; set; }
    public string ServiceInfo1 { get; set; }
    public string ServiceInfo2 { get; set; }
    public string ServiceName { get; set; } // added in V3
    public string ServiceTier { get; set; } // added in V3
    public string StoreServiceIdentifier { get; set; }
    public Guid SubscriptionGuid { get; set; }
    public long SubscriptionId { get; set; } // obsolete since V3
    public string SubscriptionName { get; set; }
    public string Tags { get; set; }
    public string UnitOfMeasure { get; set; }
}
