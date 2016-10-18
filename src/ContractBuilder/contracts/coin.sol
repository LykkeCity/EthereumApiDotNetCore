pragma solidity ^0.4.1;
contract Coin {

    address _owner;
    address _exchangeContractAddress;
    uint _lastPing;
    mapping (address => uint) public coinBalanceMultisig;
    mapping (uint => bool) public transactions;

    event CoinCashIn(address caller, uint amount);
    event CoinCashOut(address caller, address from, uint amount, address to);
    event CoinTransfer(address caller, address from, address to, uint amount);

    modifier onlyowner { if (msg.sender == _owner) _; }
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
    function cashin(uint id, address receiver, uint amount, bytes params) onlyowner payable { throw; }

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

    function ping() {
        _lastPing = now;
    }
}