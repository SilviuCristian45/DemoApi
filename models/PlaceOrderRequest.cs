public record AddressRequest (
    string City,
    string Street,
    int StreetNumber,
    int ZipCode
);

public record PlaceOrderRequest (
    List<CartItem> Items,
    AddressRequest Address,    
    string PhoneNumber,
    string PaymentType
);