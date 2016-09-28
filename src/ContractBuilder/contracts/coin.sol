pragma solidity ^0.4.1;
contract Coin {

    address _owner;
    address _exchangeContractAddress;

    mapping (address => uint) public coinBalanceMultisig;

    event CoinCashIn(address caller, uint amount);
    event CoinCashOut(address caller, address from, uint amount, address to);
    event CoinTransfer(address caller, address from, address to, uint amount);

    modifier onlyowner { if (msg.sender == _owner) _; }
    modifier onlyFromExchangeContract { if (msg.sender == _exchangeContractAddress) _; }

    function Coin(address exchangeContractAddress) {
        _owner = msg.sender;
        _exchangeContractAddress = exchangeContractAddress;
    }

    function() { throw; }

    function changeExchangeContract(address newContractAddress) onlyFromExchangeContract {
        _exchangeContractAddress = newContractAddress;
    }

    // transfer coins (called only from exchange contract)
    function transferMultisig(address from, address to, uint amount, bytes32 hash, bytes client_a_sig) onlyFromExchangeContract {
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
    function cashin(address receiver, uint amount) onlyowner { throw; }

    // virtual method (if not implemented, then throws)
    function cashout(address from, address to, uint amount, bytes32 hash, bytes client_sig) onlyFromExchangeContract { throw; }

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
}