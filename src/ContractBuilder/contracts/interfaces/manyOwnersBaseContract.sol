pragma solidity ^0.4.9;

contract ManyOwnersBaseContract {

    function AddOwners(address[] owners) returns (bool isOk);
    function RemoveOwners(address[] owners) returns (bool isOk);
    function IsOwner(address ownerAddress) returns (bool isOwner);
}