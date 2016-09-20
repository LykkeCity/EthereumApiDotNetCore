import "./mainContract.sol";

contract UserContract {
    MainContract mainContract;

    function UserContract(address mainContractAddress) {
        mainContract = MainContract(mainContractAddress);
    }

    function() {
        mainContract.gotPayment(this, msg.value);
    }

    function transferMoney(address recepient, uint value) {
        recepient.send(value);
    }
}