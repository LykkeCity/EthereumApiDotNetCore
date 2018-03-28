pragma solidity ^0.4.21;

import "./erc20Contract.sol";

/* @title Invoice smartcontract */
contract Invoice {

    //Possible Invoice statuses
    enum InvoiceStatus {UNPAID,PAID,OVERDUE,LATE_PAYMENT,PARTIALLY_PAID}

    address _owner;
    string _invoiceId; 
    address _allowedTokenAddress;
    uint _dueDate;
    uint _lastPaymentDate;
    uint _amount;
    address _merchantAirlinesWalletAddress;

    modifier onlyOwner { 
        if (msg.sender == _owner) _; 
    }

    /**@dev Invoice smartcontract constructor
        * @param invoiceId Invoice Id in external system.
        * @param amount The amount in specific token.
        * @param dueDate Unix timestamp of the latest acceptable date.
        * @param allowedTokenAddress Address of erc223 compatible token, currency of the current invoice.(The token should be erc223 compatible)
        * @param merchantAirlinesWalletAddress Address of the merchants private wallet, which accepts an invoice payment.
    */
    function Invoice(string invoiceId, uint amount, uint dueDate, address allowedTokenAddress, address merchantAirlinesWalletAddress) public {
        _owner = msg.sender;
        _invoiceId = invoiceId;
        _dueDate = dueDate;
        _allowedTokenAddress = allowedTokenAddress;
        _amount = amount;
        _merchantAirlinesWalletAddress = merchantAirlinesWalletAddress;
        _lastPaymentDate = 0;
    }

    /**@dev Function for erc223 compatibility. It sets the last payment date.
    */
    function tokenFallback(address _from, uint _value, bytes _data) public returns(bool ok) {
        address tokenAddress = msg.sender;
        if (tokenAddress != _allowedTokenAddress || _value == 0){
            revert();
        }

        _lastPaymentDate = block.timestamp;

        return true;
    }

    /**@dev returns actual InvoiceStatus
        * @return status The calculated current status.
    */
    function getInvoiceStatus() public view returns (InvoiceStatus status){
        ERC20Interface erc20Contract = ERC20Interface(_allowedTokenAddress);
        uint balance = erc20Contract.balanceOf(this);
        bool isOverdue = (_dueDate - _lastPaymentDate) < 0;

        if (_amount >= balance){
            if (!isOverdue){
                return InvoiceStatus.PAID;
            } else{
                return InvoiceStatus.LATE_PAYMENT;
            }
        }

        if (balance == 0){
            if (!isOverdue){
                return InvoiceStatus.UNPAID;
            } else{
                return InvoiceStatus.OVERDUE;
            }
        }

        if (_amount < balance){
            if (!isOverdue){
                return InvoiceStatus.PARTIALLY_PAID;
            } else{
                return InvoiceStatus.OVERDUE;
            }
        }  
    }

    /**@dev Transfers tokens from this invoice smart contract to the merchants address. Initiated by contract owner. 
        *@returns success Indicator wheter or not transfer was succesful. 
    */
    function transferAllTokens() onlyOwner public returns (bool success) {
        ERC20Interface erc20Contract = ERC20Interface(_allowedTokenAddress);
        uint balance = erc20Contract.balanceOf(this); 

        if (balance <= 0) {
            return false;
        }

        return erc20Contract.transfer(_merchantAirlinesWalletAddress, balance);
    }
}   