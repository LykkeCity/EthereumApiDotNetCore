pragma solidity ^0.4.9;
contract Coin {

    address _owner;
    address _exchangeContractAddress;
    uint _lastPing;
    mapping (address => uint) public coinBalanceMultisig;
    mapping (address => address) public transferContractUser;

    event CoinCashIn(address caller, uint amount);
    event CoinCashOut(address caller, address from, uint amount, address to);
    event CoinTransfer(address caller, address from, address to, uint amount);

    modifier onlyowner { if (msg.sender == _owner) _; }
    modifier ownerOrTransferContract { if (msg.sender == _owner || transferContractUser[msg.sender] != address(0)) _; }
    modifier onlyFromExchangeContract { if (msg.sender == _exchangeContractAddress || (now - _lastPing) > 30 days) _; }

    function Coin(address exchangeContractAddress) {
        _owner = msg.sender;
        _exchangeContractAddress = exchangeContractAddress;
    }   

    function changeExchangeContract(address newContractAddress) onlyFromExchangeContract {
        _exchangeContractAddress = newContractAddress;
    }

    // transfer coins (called only from exchange contract)
    function transferMultisig(address from, address to, uint amount, bytes32 hash, bytes client_a_sig, bytes params) onlyFromExchangeContract {
        if (!_checkClientSign(from, hash, client_a_sig)) {
            throw;
        }
        
        if (coinBalanceMultisig[from] < amount) {
            throw;
        }

        coinBalanceMultisig[from] -= amount;
        coinBalanceMultisig[to] += amount;

        CoinTransfer(msg.sender, from, to, amount);
    }

    // virtual method (if not implemented, then throws)
    function cashin(address receiver, uint amount) ownerOrTransferContract payable returns(bool) { return false; }

    // virtual method (if not implemented, then throws)
    function cashout(address from, address to, uint amount, bytes32 hash, bytes client_sig, bytes params) onlyFromExchangeContract { throw; }

    function _checkClientSign(address client_addr, bytes32 hash, bytes sig) returns(bool) {
        bytes32 r;
        bytes32 s;
        uint8 v;

        assembly {
            r := mload(add(sig, 32))
            s := mload(add(sig, 64))
            v := mload(add(sig, 65))
        }

        return client_addr == ecrecover(hash, v, r, s);
    }

    function balanceOf(address owner) constant returns(uint) {
         var balance = coinBalanceMultisig[owner];

         return balance;
    }

    function getTransferAddressUser(address transferAddress) constant returns(address){
         var userAddress = transferContractUser[transferAddress];

         return userAddress;
    }

    function setTransferAddressUser(address userAddress, address transferAddress) onlyowner{
         var oldUserAddress = transferContractUser[transferAddress];
         
         if (oldUserAddress != address(0)) {
             throw;
         }

         transferContractUser[transferAddress] = userAddress;
    }

    function ping() {
        _lastPing = now;
    }
}