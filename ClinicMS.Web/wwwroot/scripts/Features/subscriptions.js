// Subscriptions JS
var packageDurations = {'Basic Monthly':1,'Pro Monthly':1,'Elite Monthly':1,'Basic 3-Month':3,'Pro 6-Month':6,'Annual':12};
var packagePrices = {'Basic Monthly':300,'Pro Monthly':500,'Elite Monthly':800,'Basic 3-Month':800,'Pro 6-Month':2500,'Annual':4500};

var subscriptions = [
    {id:1,member:'Mahmoud Samir',pkg:'Pro Monthly',start:'2026-07-01',end:'2026-07-31',amount:500,payStatus:'Paid',status:'Active',branch:'Main Branch'},
    {id:2,member:'Rana Youssef',pkg:'Basic Monthly',start:'2026-07-05',end:'2026-08-04',amount:300,payStatus:'Paid',status:'Active',branch:'Main Branch'},
    {id:3,member:'Tamir Ahmed',pkg:'Elite Monthly',start:'2026-06-20',end:'2026-07-19',amount:800,payStatus:'Paid',status:'Active',branch:'North Branch'},
    {id:4,member:'Noha Khalil',pkg:'Annual',start:'2026-01-01',end:'2026-12-31',amount:4500,payStatus:'Paid',status:'Active',branch:'South Branch'},
    {id:5,member:'Adel Hassan',pkg:'Pro 6-Month',start:'2026-04-01',end:'2026-09-30',amount:2500,payStatus:'Paid',status:'Active',branch:'Main Branch'},
    {id:6,member:'Samar Ibrahim',pkg:'Basic Monthly',start:'2026-05-01',end:'2026-05-31',amount:300,payStatus:'Paid',status:'Expired',branch:'East Branch'},
    {id:7,member:'Karim Mostafa',pkg:'Pro Monthly',start:'2026-07-10',end:'2026-08-09',amount:500,payStatus:'Paid',status:'Active',branch:'Main Branch'},
    {id:8,member:'Amira Samy',pkg:'Basic 3-Month',start:'2026-05-01',end:'2026-07-31',amount:800,payStatus:'Partial',status:'Active',branch:'North Branch'},
    {id:9,member:'Ziad Omar',pkg:'Elite Monthly',start:'2026-06-22',end:'2026-07-21',amount:800,payStatus:'Paid',status:'Active',branch:'Main Branch'},
    {id:10,member:'Dalia Nasser',pkg:'Annual',start:'2026-02-01',end:'2027-01-31',amount:4500,payStatus:'Paid',status:'Active',branch:'South Branch'},
    {id:11,member:'Bassem Fawzy',pkg:'Pro Monthly',start:'2026-07-15',end:'2026-08-14',amount:500,payStatus:'Paid',status:'Active',branch:'East Branch'},
    {id:12,member:'Yasmine Ali',pkg:'Basic Monthly',start:'2026-04-01',end:'2026-04-30',amount:300,payStatus:'Paid',status:'Expired',branch:'Main Branch'},
    {id:13,member:'Sherif Mahmoud',pkg:'Pro 6-Month',start:'2026-03-01',end:'2026-08-31',amount:2500,payStatus:'Paid',status:'Active',branch:'North Branch'},
    {id:14,member:'Reem Hassan',pkg:'Elite Monthly',start:'2026-07-01',end:'2026-07-31',amount:800,payStatus:'Partial',status:'Active',branch:'Main Branch'},
    {id:15,member:'Nabil Ibrahim',pkg:'Annual',start:'2025-07-01',end:'2026-06-30',amount:4500,payStatus:'Paid',status:'Expired',branch:'South Branch'},
    {id:16,member:'Heba Mostafa',pkg:'Basic Monthly',start:'2026-07-14',end:'2026-08-13',amount:300,payStatus:'Paid',status:'Active',branch:'East Branch'},
    {id:17,member:'Wael Salem',pkg:'Pro Monthly',start:'2026-07-01',end:'2026-07-31',amount:500,payStatus:'Unpaid',status:'Active',branch:'Main Branch'},
    {id:18,member:'Mona Youssef',pkg:'Basic 3-Month',start:'2026-06-01',end:'2026-08-31',amount:800,payStatus:'Paid',status:'Active',branch:'North Branch'}
];

var currentPage=1, perPage=8, searchQuery='', statusF='', editingId=null;
var payStatusBadge={'Paid':'gp-badge-green','Partial':'gp-badge-yellow','Unpaid':'gp-badge-red'};
var subStatusBadge={'Active':'gp-badge-green','Expired':'gp-badge-red','Cancelled':'gp-badge-gray'};

function filtered(){
    var q=searchQuery.toLowerCase();
    return subscriptions.filter(s=>(!q||s.member.toLowerCase().includes(q)||s.pkg.toLowerCase().includes(q))&&(!statusF||s.status===statusF));
}

function renderTable(){
    var data=filtered();var total=data.length;
    var pages=Math.ceil(total/perPage)||1;if(currentPage>pages)currentPage=1;
    var slice=data.slice((currentPage-1)*perPage,currentPage*perPage);
    document.getElementById('subsTableBody').innerHTML=slice.length?slice.map((s,i)=>`
        <tr>
            <td>${(currentPage-1)*perPage+i+1}</td>
            <td><span style="font-weight:700;">${s.member}</span></td>
            <td>${s.pkg}</td>
            <td>${s.start}</td>
            <td>${s.end}</td>
            <td><span style="font-weight:700;color:#0d9488;">${s.amount.toLocaleString()} EGP</span></td>
            <td><span class="gp-badge ${payStatusBadge[s.payStatus]||'gp-badge-gray'}">${s.payStatus}</span></td>
            <td><span class="gp-badge ${subStatusBadge[s.status]||'gp-badge-gray'}">${s.status}</span></td>
            <td>${s.branch}</td>
            <td><div style="display:flex;gap:6px;">
                <button class="gp-btn-icon gp-btn-edit" onclick="renewSub(${s.id})" title="Renew"><i class="ri-refresh-line"></i></button>
                <button class="gp-btn-icon gp-btn-delete" onclick="cancelSub(${s.id})" title="Cancel"><i class="ri-close-circle-line"></i></button>
            </div></td>
        </tr>`).join(''):'<tr><td colspan="10" class="text-center py-4 text-muted">No subscriptions found</td></tr>';
    document.getElementById('pageInfo').textContent=`Showing ${slice.length} of ${total}`;
    var btns=document.getElementById('pageBtns');btns.innerHTML='';
    for(var p=1;p<=pages;p++){var btn=document.createElement('button');btn.className='gp-page-btn'+(p===currentPage?' active':'');btn.textContent=p;btn.onclick=(function(pp){return function(){currentPage=pp;renderTable();};})(p);btns.appendChild(btn);}
    document.getElementById('statActive').textContent=subscriptions.filter(s=>s.status==='Active').length;
    var now=new Date();
    document.getElementById('statExpiring').textContent=subscriptions.filter(s=>{var d=new Date(s.end);return s.status==='Active'&&(d-now)/(1000*60*60*24)<=7&&d>=now;}).length;
    document.getElementById('statExpired').textContent=subscriptions.filter(s=>s.status==='Expired').length;
    var rev=subscriptions.reduce((a,s)=>a+s.amount,0);
    document.getElementById('statRevenue').textContent=rev.toLocaleString()+' EGP';
}

function openModal(){
    document.getElementById('modalTitle').textContent='New Subscription';
    document.getElementById('fStart').value=new Date().toISOString().slice(0,10);
    calcEndDate();
    new bootstrap.Modal(document.getElementById('subModal')).show();
}

function updatePrice(){
    var pkg=document.getElementById('fPackage').value;
    document.getElementById('fAmount').value=packagePrices[pkg]||0;
    calcEndDate();
}

function calcEndDate(){
    var start=document.getElementById('fStart').value;
    var pkg=document.getElementById('fPackage').value;
    if(!start)return;
    var months=packageDurations[pkg]||1;
    var d=new Date(start);
    d.setMonth(d.getMonth()+months);
    d.setDate(d.getDate()-1);
    document.getElementById('fEnd').value=d.toISOString().slice(0,10);
}

function saveSubscription(){
    var member=document.getElementById('fMember').value;
    var start=document.getElementById('fStart').value;
    if(!member||!start){toastr.error('Fill required fields');return;}
    subscriptions.push({
        id:Date.now(),member,pkg:document.getElementById('fPackage').value,
        start,end:document.getElementById('fEnd').value,
        amount:parseFloat(document.getElementById('fAmount').value)||0,
        payStatus:'Paid',status:'Active',
        branch:document.getElementById('fBranch').value
    });
    toastr.success('Subscription created');
    bootstrap.Modal.getInstance(document.getElementById('subModal')).hide();
    renderTable();
}

function renewSub(id){
    var s=subscriptions.find(x=>x.id===id);
    var months=packageDurations[s.pkg]||1;
    var d=new Date(s.end);d.setDate(d.getDate()+1);
    var newEnd=new Date(d);newEnd.setMonth(newEnd.getMonth()+months);newEnd.setDate(newEnd.getDate()-1);
    subscriptions.push({...s,id:Date.now(),start:d.toISOString().slice(0,10),end:newEnd.toISOString().slice(0,10),status:'Active'});
    toastr.success('Subscription renewed');renderTable();
}

function cancelSub(id){
    if(!confirm('Cancel this subscription?'))return;
    var s=subscriptions.find(x=>x.id===id);s.status='Cancelled';
    toastr.success('Cancelled');renderTable();
}

function handleSearch(v){searchQuery=v;currentPage=1;renderTable();}
function handleFilter(){statusF=document.getElementById('statusFilter').value;currentPage=1;renderTable();}
document.addEventListener('DOMContentLoaded',function(){
    document.getElementById('fStart').value=new Date().toISOString().slice(0,10);
    updatePrice();
    renderTable();
});

// -- Select2 init --
function initSelect2() {
    if (typeof $ !== 'undefined' && $.fn && $.fn.select2) {
        $('select.form-select, select.gp-select').select2({
            theme: 'default',
            width: '100%',
            dropdownParent: document.body
        });
    }
}
// Re-run on each modal open
document.addEventListener('show.bs.modal', function() { setTimeout(initSelect2, 50); });
document.addEventListener('DOMContentLoaded', initSelect2);
