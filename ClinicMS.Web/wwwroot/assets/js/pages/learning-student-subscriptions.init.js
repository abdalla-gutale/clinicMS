

//subscriptionList Table
var options = {
    valueNames: [
        "plan",
        "price",
        "duration",
        "status",
        "payment_due"
    ],
};

// Init list
var subscriptionList = new List("subscriptionList", options).on("updated", function (list) {
    if(document.getElementsByClassName("noresult") && document.getElementsByClassName("noresult")[0]) {
        list.matchingItems.length == 0 ?
            (document.getElementsByClassName("noresult")[0].style.display = "block") :
            (document.getElementsByClassName("noresult")[0].style.display = "none");

        if (list.matchingItems.length > 0) {
            document.getElementsByClassName("noresult")[0].style.display = "none";
        } else {
            document.getElementsByClassName("noresult")[0].style.display = "block";
        }
    }
});