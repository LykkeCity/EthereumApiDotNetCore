pragma solidity ^0.4.1;
contract MainContract {

    event PaymentFromUser(address userAddress, uint amount);

    function MainContract(){}

    function gotPayment(address userContractAddress, uint amount) {
        PaymentFromUser(userContractAddress, amount);
    }
}