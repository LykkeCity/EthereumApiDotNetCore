//DO NOT USE THIS FOR PRODUCTION

pragma solidity ^0.4.8;


contract Erc223FailRandom {

    uint8[] public numbers;
        
    function Erc223FailRandom() public {

    }

    function tokenFallback(address _from, uint _value, bytes _data) public view returns(bool ok) {
        uint8 randomValue = random();
        if (randomValue == 1)
            throw;

        return true;
    }
    
    function random() private view returns (uint8) {
        return uint8(uint256(keccak256(block.timestamp, block.difficulty + 2))%2);
    }
}