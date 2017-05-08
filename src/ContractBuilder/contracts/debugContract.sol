pragma solidity ^0.4.9;

contract DebugContract {
    event DebugEvent(
        int _eventNumber,
        string _value
    );

    function DebugContract(){

    }

    function getHash(uint id, address coinAddress, address client, address to, uint amount) public returns(bytes32) {
       return sha3(id, coinAddress, client, to, amount);
    }

    function checkClientSign(address client_addr, bytes32 hash, bytes sig) public returns(bool) {
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
