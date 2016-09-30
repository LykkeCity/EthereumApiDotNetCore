pragma solidity ^0.4.1;
import "./mainContract.sol";

contract UserContract {
    address _owner;
    MainContract mainContract;

    modifier onlyowner { if (msg.sender == _owner) _; }

    function UserContract(address mainContractAddress) {
        mainContract = MainContract(mainContractAddress);
        _owner = msg.sender;
    }

    function() payable {
        mainContract.gotPayment(this, msg.value);
    }

    function transferMoney(address recepient, uint value) onlyowner {
        if (!recepient.send(value))
            throw;
    }
}